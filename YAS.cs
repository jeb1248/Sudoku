
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;
using YAS.Core;
using System.ComponentModel;

//  Yet Another Sudoku

/*
 * Here.  You can copy this, or a portion thereof, becasue it's used so often
            
            for(int i=0; i<9; i++)
            {
                for(int j=0; j<9; j++)
                {
                    for(int k=0; k<9; k++)
                    {

                    }
                }
            }

    Or like this for just i and j
            for(int i=0; i<9; i++)            
                for(int j=0; j<9; j++)
                {
                }
            

*/

namespace YAS
{
    public partial class FrmYAS : Form
    {
        #region Overhead Declarations
        
        Stopwatch stopwatch;    // This is used by Solve
        Solver solver;          // This is used by Solve
        Panel NumBox;
        Panel ShadowBox;
        Button btnExit;

        // Storage for the HKEY_Current_User\SOFTWARE\ProgName RegistryKey
        RegistryKey rkProgName = null;
        // Obtain an instance of RegistryKey for the CurrentUser registry root. 
        // RegistryKey rkCurrentUser = Registry.CurrentUser;
        String SubKeyName = "LastDirectory";
        string LastDir;

        //REMINDER: tuples can only handle 7 Item declarations without special formatting
        List<Tuple<int, int>> MultiSelection = new List<Tuple<int, int>>();     // Not really used yet

        public static List<int> CandidateList = new List<int>();

        int FilterActive = -1;
        int LastFilter = -1;
        string LastFileName = "";
        bool GenFailed = false;
        bool KeyShifted = false;
        bool KeyControlled = false;
        bool PuzzleEdit = false;
        public string difficulty = "Easy";
        public int DifficultyIdx = 0;
        public int offset = 3;

        const int NumFilterBtn = 11;
        const int NumActionBtn = 8;
        static Button[] filterButton = new Button[NumFilterBtn];
        static Button[] actionButton = new Button[NumActionBtn];
        public static Label[] myNumBtn = new Label[9];

        public TextBox tbDifficulty = new TextBox();
        public TextBox tbStatus = new TextBox();
        public TextBox tbTime = new TextBox();

        Label[] RowAlphas = new Label[9];
        Label[] ColumnNums = new Label[9];

        public int[,] temppuz;
        static public int[,] puzzle;
        static public int[,] solution;

        static public int[][] kPuzzle = Utils.CreateJaggedArray<int[][]>(9, 9);

        string[] Alpha = { "A", "B", "C", "D", "E", "F", "G", "H", "I" };
        Keys[] Directn = { Keys.Up, Keys.Down, Keys.Left, Keys.Right };
        Keys[] NumKey = { Keys.D0, Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9 };

        // Use this as the enumeration of the Solve Methods.  Then if you want to Add/Delete/ChangeTheOrder 
        // of the solvers, you just change the list and not every instance of the entries.
        // This is the model of the entry: (int)SolveMethods.Zero
        public enum SolveMethods { Zero, SolveDLM, SolveBacktracking, SolveNakedSingles, FindHiddenSingles, Kermalis };

        // Use this as the enumeration of the Action Buttons
        public enum ActionButOrd { GenPuz, PuzDif, ChkPuz, ShowStep, NkdSing, HidSing, DLM, Kermalis };

        /*
            Use this as the enumeration of the Colors used.  
            KEEP THIS COORDINATED WITH "ColorDefs" or the display will be very strange.
            (int)ColorsUsed.blu
        */
        public enum ColorsUsed { wht, blk, blu, grn, yel, pnk, org, spgrn,
            grnyel, ltpnk, ltcyan, ltgrn, ltyel, ltgray, dkblu, dsblu
        };

        static public Tuple<Color, ColorsUsed>[] ColorDefs =
        {   // Colors, ColorsUsed           
            Tuple.Create( Color.White,      ColorsUsed.wht),
            Tuple.Create( Color.Black,      ColorsUsed.blk),
            Tuple.Create( Color.Blue,       ColorsUsed.blu),
            Tuple.Create( Color.Green,      ColorsUsed.grn),
            Tuple.Create( Color.Yellow,     ColorsUsed.yel),
            Tuple.Create( Color.Pink,       ColorsUsed.pnk),
            Tuple.Create( Color.Orange,     ColorsUsed.org),   // Orange = Amber
            Tuple.Create( Color.SpringGreen,ColorsUsed.spgrn ),
            Tuple.Create( Color.GreenYellow,ColorsUsed.grnyel ),
            Tuple.Create( Color.LightPink,  ColorsUsed.ltpnk ),
            Tuple.Create( Color.LightCyan,  ColorsUsed.ltcyan ),
            Tuple.Create( Color.LightGreen, ColorsUsed.ltgrn ),
            Tuple.Create( Color.LightYellow,ColorsUsed.ltyel ),
            Tuple.Create( Color.LightGray,  ColorsUsed.ltgray ),
            Tuple.Create( Color.DarkBlue,   ColorsUsed.dkblu ),
            Tuple.Create( Color.DeepSkyBlue,   ColorsUsed.dsblu )
        };

        #endregion

        #region Overhead Methods

        public FrmYAS()
        {
            InitializeComponent();
        }   //  End frmDLM

        private void FrmYAS_Load(object sender, EventArgs e)
        {
            Top = 5;
            Left = 150;
            MoveStuff();        // Get the display as we want it
            InitRegistryVars();     // Get this out of the way
            InitTimer();

            MainPanel.SuspendLayout();  // Stop MainPanel updates
            SuspendLayout();            // Stop Form updates

            BoxesSetup();               // Set up the Boxes 
            SolutionsSetup();           // Set up the Solution Cells
            CandidatesSetup();          // Set up the Candidate numbers
            PuzzleEditPanelSetup();     // Set up the panel for entering puzzle numbers
            MoveMoreStuff();            // Set up the Row and Column name panels
            MainPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();

            this.KeyUp += new System.Windows.Forms.KeyEventHandler(KeyUpEvent);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(KeyDownEvent);

            tbDifficulty.Text = difficulty;

            GeneratePuzzle();       // Generate an Easy puzzle
        }   // End FrmDLM_Load

        private void KeyUpEvent(object sender, KeyEventArgs e) //Keyup Event 
        {
            switch (e.KeyCode)
            {
                case Keys.ShiftKey:
                    KeyShifted = false;
                    break;
                case Keys.ControlKey:
                    KeyControlled = false;
                    break;
                case Keys.F2:   // File->Open
                    LoadPuzzle();
                    break;
                case Keys.F7:   // Actions->Hints
                    SolvePuzzleMethods((int)SolveMethods.Kermalis, false); break;
                case Keys.F11:   // Actions->ShowNextStep
                    ShowNextStep();
                    break;
                case Keys.F12:   // Actions->SolveAllNakedSingles
                    Solvers.SolveNakedSingles();
                    ResetFilters();
                    break;
                case Keys.Escape:
                    if (KeyControlled == true)
                    {
                        if (PuzzleEdit == true)
                        {
                            PuzzleEdit = false;
                            ProcessNewPuzzle();
                        }
                    }
                    else
                    {
                        PuzzleEdit = false;
                    }
                    break;
                default:    // Move to a new solution cell or fill in the last solution cell
                    if (PuzzleEdit == true)
                    {
                        int loc = Array.IndexOf(Directn, (e.KeyCode));
                        if (loc != -1)
                        {
                            Utilities.MoveCell(loc);
                        }
                        else
                        {
                            int num = Array.IndexOf(NumKey, (e.KeyCode));
                            if(( num > 0) && (KeyShifted == false))
                            {
                                mySolutions[LastSolution.X, LastSolution.Y].Text = num.ToString();
                            }
                        }

                    }   // End if

                    tbStatus.Text = (e.KeyCode.ToString());
                    break;
            }   // End switch
        }   // End Keyup Event

        private void KeyDownEvent(object sender, KeyEventArgs e) //KeyDown Event 
        {
            switch (e.KeyCode)
            {
                case Keys.ShiftKey:
                    KeyShifted = true;
                    break;
                case Keys.ControlKey:
                    KeyControlled = true;
                    break;
                default:
                    if (KeyControlled == true)  // Control key was pressed
                    {   
                        if (KeyShifted == true)  // Shift key was pressed first
                        {
                            ShftCntrl(sender, e);
                        }
                        else 
                        {
                            Cntrl(sender, e);
                        }
                    }

                    if (KeyShifted == true)  // Shift key was pressed
                    {
                        if(KeyControlled == true)  // Control key was pressed first
                        {
                            ShftCntrl(sender, e);
                        }
                    }

                    tbStatus.Text = (e.KeyCode.ToString());
                    break;
            }
        }   // End KeyDown

        private void Cntrl(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Z:
                    MessageBox.Show("This option is not implemented yet.");
                    break;
            }
        }   // End Control

        private void ShftCntrl(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Z:
                    MessageBox.Show("This option is not implemented yet.");
                    break;
            }
       }   // End ShiftControl

        private void ResetFilters()
        {
            for (int i = 0; i < filterButton.Length; i++)
            { filterButton[i].BackColor = Color.White; }

            if(FilterActive>0)
            {
                ActivateFilter(FilterActive);
            }
            else
                ClearFilters();

            UpdateStatus();

        }   // End ResetFilters

        private void InitRegistryVars()
        {
            /*  BIG NOTE:
                System.AppDomain.CurrentDomain.FriendlyName - 
                Returns the filename with extension (e.g. MyApp.exe).

                System.Diagnostics.Process.GetCurrentProcess().ProcessName - 
                Returns the filename without extension (e.g. MyApp).

                System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName - 
                Returns the full path and filename (e.g. C:\Examples\Processes\MyApp.exe). 
                You could then pass this into System.IO.Path.GetFileName() or 
                System.IO.Path.GetFileNameWithoutExtension() to achieve the same results as the above.
             */
            //  Get the name of the program to use as a register location
            String ProgramName = Process.GetCurrentProcess().ProcessName;
            RegistryKey SoftSubKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            RegistryKey rkProgName = SoftSubKey.OpenSubKey(ProgramName, true);

            if (rkProgName == null)
            {
                // Set up the Registry Key for the ProgName under HKEY_Current_User\SOFTWARE
                rkProgName = SoftSubKey.CreateSubKey(ProgramName);
                rkProgName.SetValue(SubKeyName, "C:\\sc");
            }

            LastDir = rkProgName.GetValue(SubKeyName).ToString(); 

        }   // End InitRegistryVars

        private void FrmYAS_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (rkProgName == null)
            {
                String ProgramName = Process.GetCurrentProcess().ProcessName;
                RegistryKey SoftSubKey = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
                RegistryKey rkProgName = SoftSubKey.OpenSubKey(ProgramName, true);
                rkProgName.SetValue(SubKeyName, LastDir);
            }
            else 
            {
                rkProgName.SetValue(SubKeyName, LastDir);
            }
        }   // End FrmYAS_FormClosing

        private void MainPanel_Paint(object sender, PaintEventArgs e)
        {
            Color col = ColorDefs[(int)ColorsUsed.dkblu].Item1;
            ButtonBorderStyle bbs = ButtonBorderStyle.Solid;
            int thickness = 4;
            ControlPaint.DrawBorder(e.Graphics, this.MainPanel.ClientRectangle, col, thickness, bbs, col, thickness, bbs, col, thickness, bbs, col, thickness, bbs);
        }   //  End panel1_Paint

        void MoveStuff()
        {
            // This Method id fair sized.  It defines a lot of the panels' contents and 
            // positions everything on the display.
            this.Width = 835;
            this.Height = 740;

            Font myFont = new Font("Microsoft Sans Serif", 10); // Default Font size

            // Create the ToolTip and associate with the Form container.
            ToolTip toolTip1 = new ToolTip();

            // Set up the delays for the ToolTip.
            toolTip1.AutoPopDelay = 5000;
            toolTip1.InitialDelay = 1000;
            toolTip1.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip1.ShowAlways = true;

            //  Adjust Height at the end
            filterButtonPanel.Top = menuStrip1.Bottom + 5;
            filterButtonPanel.Left = offset;
            filterButtonPanel.Height = 35;

            RowPanel.Left = filterButtonPanel.Left;
            RowPanel.Width = (int)(filterButtonPanel.Height * .65);
            RowPanel.BorderStyle = BorderStyle.None;
            ColumnPanel.Top = filterButtonPanel.Bottom + offset;
            ColumnPanel.Height = RowPanel.Width;
            ColumnPanel.BorderStyle = BorderStyle.None;

            // Main Panel Setup
            //            MainPanel.Width = 556;
            MainPanel.Width = 530;
            MainPanel.Left = RowPanel.Right + offset;
            MainPanel.Height = MainPanel.Width;             // Make sure this is square
            MainPanel.Top = ColumnPanel.Bottom + offset;

            RowPanel.Top = MainPanel.Top;
            RowPanel.Height = MainPanel.Height;
            ColumnPanel.Left = MainPanel.Left;
            ColumnPanel.Width = MainPanel.Width;

            logList.Top = MainPanel.Top;
            logList.Height = MainPanel.Height;
            logList.Width = MainPanel.Width;
            logList.Left = MainPanel.Right + (2 * offset);

            filterButtonPanel.Width = MainPanel.Width;
            string wd = "wd= " + filterButtonPanel.Width.ToString();

            {   // Filter Button Setup

                int ButtonSize = filterButtonPanel.Height - (4 * offset);

                for (int i = 0; i < NumFilterBtn; i++)
                {
                    filterButton[i] = new Button()
                    { 
                        BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,
                        Size = new System.Drawing.Size(ButtonSize, ButtonSize),
                        Name = "btnFilter" + i.ToString(),
                        Font = myFont,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Visible = true,
                        Tag = i,
                        Enabled = true
                    };

                    filterButton[i].Height = filterButtonPanel.Height - (offset * 2);
                    filterButton[i].Width = filterButton[i].Height;
                    filterButton[i].Top = filterButtonPanel.Top + offset;
                    filterButton[i].Show();

                    //  Line buttons up in a tidy row
                    switch (i)
                    {
                        case 0: // Clear Filters button on the Left
                            filterButton[i].Left = filterButtonPanel.Left + offset;
                            toolTip1.SetToolTip(filterButton[i], "Clear Filters");
                            break;
                        case 10:    // Filter on Pairs button on the Right
                            filterButton[i].Left = filterButton[i - 1].Right + offset;
                            filterButton[i].Image = YAS.Properties.Resources.gem8;
                            toolTip1.SetToolTip(filterButton[i], "Filter on Pairs");
                            break;
                        default:    // Number buttons in order between 0 and 10
                            filterButton[i].Left = filterButton[i - 1].Right + offset;
                            filterButton[i].Text = i.ToString();
                            toolTip1.SetToolTip(filterButton[i], "Filter on " + i.ToString() + " 's");
                            break;
                    }

                    Controls.Add(filterButton[i]);
                    filterButton[i].Click += new EventHandler(BtnFilter_Click);
                    filterButton[i].BringToFront();
                }   // End for i
            }   // End Filter Button Setup

            filterButtonPanel.Width = filterButton[NumFilterBtn - 1].Right + offset;

            {   // actionButtonPanel Setup
                actionButtonPanel.Left = filterButtonPanel.Right + (offset);
                actionButtonPanel.Width = MainPanel.Width - filterButtonPanel.Width;
                actionButtonPanel.Height = filterButtonPanel.Height;
                actionButtonPanel.Top = filterButtonPanel.Top;
            }   // End actionButtonPanel Setup

            {   // Action Button Setup
                int ButtonSize = actionButtonPanel.Height - (4 * offset);

                for (int i = 0; i < NumActionBtn; i++)
                {
                    actionButton[i] = new Button()
                    {
                        BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,
                        Size = new System.Drawing.Size(ButtonSize, ButtonSize),
                        Name = "btnAction" + i.ToString(),
                        Font = myFont,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Tag = i,
                        Visible = true,
                        Enabled = true
                    };

                    actionButton[i].Height = actionButtonPanel.Height - (offset * 2);
                    actionButton[i].Width = actionButton[i].Height;

                    actionButton[i].Top = actionButtonPanel.Top + offset;
                    actionButton[i].Show();

                    //  Line buttons up in a tidy row
                    if (i == 0) { actionButton[i].Left = actionButtonPanel.Left + offset; }
                    else { actionButton[i].Left = actionButton[i - 1].Right + offset; }

                    Controls.Add(actionButton[i]);
                    actionButton[i].Click += new EventHandler(BtnAction_Click);
                    actionButton[i].BringToFront();

                    switch (i)
                    {
                        case (int)ActionButOrd.GenPuz:
                            actionButton[i].Image = YAS.Properties.Resources.grid1;
                            toolTip1.SetToolTip(actionButton[i], "Generate Puzzle");
                            break;
                        case (int)ActionButOrd.PuzDif:
                            actionButton[i].Image = YAS.Properties.Resources.hammer2;
                            toolTip1.SetToolTip(actionButton[i], "Select Puzzle Difficulty");
                            break;
                        case (int)ActionButOrd.ChkPuz:
                            actionButton[i].Image = YAS.Properties.Resources.chkmark;
                            toolTip1.SetToolTip(actionButton[i], "Check Completed Puzzle");
                            break;
                        case (int)ActionButOrd.ShowStep:
                            actionButton[i].Image = YAS.Properties.Resources.qmark3;
                            toolTip1.SetToolTip(actionButton[i], "Show Next Step in the Status Box.");
                            break;
                        case (int)ActionButOrd.NkdSing:
                            actionButton[i].Image = YAS.Properties.Resources.NakedSingle;
                            toolTip1.SetToolTip(actionButton[i], "Solve Naked Singles");
                            break;
                        case (int)ActionButOrd.HidSing:
                            actionButton[i].Image = YAS.Properties.Resources.HiddenSingle;
                            toolTip1.SetToolTip(actionButton[i], "Solve Hidden Singles");
                            break;
                        case (int)ActionButOrd.DLM:
                            actionButton[i].Image = YAS.Properties.Resources.ballerina3;
                            toolTip1.SetToolTip(actionButton[i], "Solve Using Dancing Links");
                            break;
                        case (int)ActionButOrd.Kermalis:
                            actionButton[i].Image = YAS.Properties.Resources.book1;
                            toolTip1.SetToolTip(actionButton[i], "Solve Current Display Using Advanced Methods");
                            break;
                        default:
                            actionButton[i].Visible = false;
                            actionButton[i].Enabled = false;
                            break;
                    }
                }   // End for i
            }   // End Action Button Setup

            actionButtonPanel.Width = (NumActionBtn * actionButton[0].Width) + (offset * (NumActionBtn + 1));

            StatusPanel.Left = actionButtonPanel.Right + offset;
            StatusPanel.Top = actionButtonPanel.Top;
            StatusPanel.Height = actionButtonPanel.Height;
            StatusPanel.Width = logList.Right - StatusPanel.Left;

            this.Height = MainPanel.Bottom + 40;

            tbDifficulty.Font = myFont;
            tbDifficulty.WordWrap = false;
            tbDifficulty.ForeColor = ColorDefs[(int)ColorsUsed.blk].Item1;
            tbDifficulty.BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
            tbDifficulty.TextAlign = HorizontalAlignment.Center;
            tbDifficulty.ReadOnly = true;
            StatusPanel.Controls.Add(tbDifficulty);

            tbStatus.Font = myFont;
            tbStatus.ForeColor = ColorDefs[(int)ColorsUsed.blk].Item1;
            tbStatus.BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
            tbStatus.WordWrap = false;
            tbStatus.ReadOnly = true;
            StatusPanel.Controls.Add(tbStatus);

            tbTime.Font = myFont;
            tbTime.ForeColor = ColorDefs[(int)ColorsUsed.blk].Item1;
            tbTime.BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
            tbTime.TextAlign = HorizontalAlignment.Center;
            tbTime.WordWrap = false;
            tbTime.ReadOnly = true;
            StatusPanel.Controls.Add(tbTime);

            tbDifficulty.Top = tbStatus.Top = tbTime.Top = offset;
            tbDifficulty.Height = tbStatus.Height = tbTime.Height = StatusPanel.Height - (2 * offset);
            tbTime.Width = tbDifficulty.Width = (int)(StatusPanel.Width / 7);
            tbStatus.Width = StatusPanel.Width - (tbTime.Width + tbDifficulty.Width + (4 * offset));

            tbDifficulty.Left = offset;
            tbStatus.Left = tbDifficulty.Right + offset;
            tbTime.Left = tbStatus.Right + offset;

            tbDifficulty.ReadOnly = true;
            tbStatus.ReadOnly = true;
            tbTime.ReadOnly = true;

            tbDifficulty.BringToFront();
            tbStatus.BringToFront();
            tbTime.BringToFront();
            toolTip1.SetToolTip(tbDifficulty, "Shows the Puzzle Difficulty Selected");
            toolTip1.SetToolTip(tbStatus, "Shows current activity status");
            toolTip1.SetToolTip(tbTime, "Shows Puzzle Time");
            /*
                        PictureBox pictureBox1 = new PictureBox();
                        pictureBox1.Image = YAS.Properties.Resources.HorizontalStripes;
                        pictureBox1.Top = tbStatus.Top + offset;
                        pictureBox1.Left = tbStatus.Left + (10 * offset);
                        pictureBox1.Width = pictureBox1.Image.Width;
                        pictureBox1.Height = pictureBox1.Image.Height;
                        pictureBox1.BringToFront();
                        pictureBox1.Visible = false;
                        pictureBox1.Enabled = false;
            */

            this.Height = MainPanel.Bottom + 45;
            this.Width = logList.Right + 20;

            Rectangle rect = Screen.FromControl(this).Bounds;

            this.Left = (rect.Width - this.Width) / 2;
            this.Top = (rect.Height - this.Height) / 4;

        }   // End MoveStuff

        void MoveMoreStuff()
        {   // OK. You have the display all set up.  Now you want to add the Row and Column labels
            // without screwing everything else up.  Good luck with that.

            // Create the ToolTip and associate with the Form container.
            ToolTip toolTip2 = new ToolTip();

            // Set up the delays for the ToolTip.
            toolTip2.AutoPopDelay = 5000;
            toolTip2.InitialDelay = 1000;
            toolTip2.ReshowDelay = 500;
            // Force the ToolTip text to be displayed whether or not the form is active.
            toolTip2.ShowAlways = true;


            int BoxSize = RowPanel.Width - 4;
            Font myFont = Utilities.TextSetup("Arial", ColorDefs[(int)ColorsUsed.blk].Item1, BoxSize, BoxSize);
            int cmin = Rows[0, 0].Top;
            int cmax = Rows[0, 0].Bottom;
            int cmid = (cmax - cmin)/2;

            for (int i = 0; i < 9; i++)
            {
                RowAlphas[i] = new System.Windows.Forms.Label()
                {
                    BorderStyle = System.Windows.Forms.BorderStyle.None,
                    Size = new System.Drawing.Size(BoxSize, BoxSize),
                    Left = RowPanel.Left + 1,
                    Name = "RA" + i.ToString(),
                    Font = myFont,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,
                    Text = Alpha[i]
                };

                toolTip2.SetToolTip(RowAlphas[i], "Row " + Alpha[i] + " ");
                RowAlphas[i].Top = cmid + (i* cmax);
                RowAlphas[i].BringToFront();
                RowAlphas[i].SuspendLayout();
                RowPanel.Controls.Add(RowAlphas[i]);
                RowAlphas[i].ResumeLayout(false);

                ColumnNums[i] = new System.Windows.Forms.Label()
                {
                    BorderStyle = System.Windows.Forms.BorderStyle.None,
                    Size = new System.Drawing.Size(BoxSize, BoxSize),
                    Top = 1,
                    Name = "CN" + i.ToString(),
                    Font = myFont,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,                  
                    Text = (i + 1).ToString()
                };

                toolTip2.SetToolTip(ColumnNums[i], "Column " + ((i+1).ToString()) + " ");
                ColumnNums[i].Left = cmid + (i * cmax);
                ColumnNums[i].BringToFront();
                ColumnNums[i].SuspendLayout();
                ColumnPanel.Controls.Add(ColumnNums[i]);
                ColumnNums[i].ResumeLayout(false);

            }        
        }   // End MoveMoreStuff

        void PuzzleEditPanelSetup()
        {
            // This is called by FrmYAS_Load() after MoveStuff() and before MoveMoreStuff()
            int NumOffset = 14;

            // PuzzleEditPanel setup
            PuzzleEditPanel.Top = logList.Top + (5 * offset);
            PuzzleEditPanel.Left = logList.Left + (5 * offset);
            PuzzleEditPanel.Height = logList.Height - (10 * offset);
            PuzzleEditPanel.Width = logList.Width - (10 * offset);
            PuzzleEditPanel.BringToFront();


            int BoxSize = (PuzzleEditPanel.Size.Width / 2);
            BoxSize -= NumOffset;

            ShadowBox = new System.Windows.Forms.Panel()
            {
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Size = new System.Drawing.Size(BoxSize, BoxSize),
                Location = new Point(NumOffset, NumOffset),
                BackColor = ColorDefs[(int)ColorsUsed.blu].Item1,
                Name = "SB"
            };
            PuzzleEditPanel.Controls.Add(ShadowBox);

            BoxSize = (ShadowBox.Size.Width);
            BoxSize -= (NumOffset);

            NumBox = new System.Windows.Forms.Panel()
            {
                BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                Size = new System.Drawing.Size(BoxSize, BoxSize),
                Location = new Point((NumOffset / 2), (NumOffset / 2)),
                BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,
                Name = "NBP"
            };
            ShadowBox.Controls.Add(NumBox);
            NumBox.BringToFront();

            BoxSize = (NumBox.Size.Width / 3) - NumOffset;
            Font NumFont = Utilities.TextSetup("Times New Roman", ColorDefs[(int)ColorsUsed.blk].Item1, 
                (BoxSize - offset), (BoxSize - offset));

            Utilities.CalcBoxPos(BoxSize, NumOffset);

            for (int i = 0; i < 9; i++)
            {
                int xoff = (i % 3) * (int)(NumOffset / 2);
                int yoff = (i / 3) * (int)(NumOffset / 2);

                myNumBtn[i] = new System.Windows.Forms.Label()
                {
                    BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                    Size = new System.Drawing.Size(BoxSize, BoxSize),
                    Location = new Point((BoxLoc[i].X + xoff), (BoxLoc[i].Y + yoff)),
                    Name = "NB" + i.ToString(),
                    Font = NumFont,
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,
                    Text = (i + 1).ToString()
                };
                NumBox.Controls.Add(myNumBtn[i]);
                myNumBtn[i].Click += new EventHandler(NumBtn_Click);
                myNumBtn[i].DoubleClick += new EventHandler(NumBtn_DoubleClick);
            }

            Font BtnFont = new Font("Microsoft Sans Serif", 12, FontStyle.Bold);

            btnClear = new Button();
            btnClear.Text = "Clear Entry";

            btnExit = new Button();
            btnExit.Text = "Exit Entry Mode";


            btnExit.Width = btnClear.Width = ShadowBox.Width - (NumOffset * 2);
            btnExit.Height = btnClear.Height = 40;
            btnExit.Font = btnClear.Font = BtnFont;

            btnClear.Top = ShadowBox.Bottom + NumOffset;
            btnExit.Top = btnClear.Bottom + NumOffset;
            btnExit.Left = btnClear.Left = ShadowBox.Left + NumOffset;
            btnClear.Click += new EventHandler(btnClear_Click);
            btnClear.DoubleClick += new EventHandler(btnClear_Click);
            btnExit.Click += new EventHandler(btnExit_Click);
            btnExit.DoubleClick += new EventHandler(btnExit_Click);

            PuzzleEditPanel.Width = ShadowBox.Width + (NumOffset * 2);
            PuzzleEditPanel.Height = btnExit.Bottom + (NumOffset * 2);
            ShadowBox.Controls.Add(NumBox);

            PuzzleEditPanel.Controls.Add(btnClear);
            PuzzleEditPanel.Controls.Add(btnExit);

            PuzzleEditPanel.Visible = false;

        }   // End PuzzleEditPanelSetup


        #endregion

        #region Cell Clicks

        private void btnClear_Click(object sender, EventArgs e)
        {
            tbStatus.Text = "Clear Entry";
            mySolutions[LastSolution.X, LastSolution.Y].Text = "";
        }   // End btnClear_Click

        private void btnExit_Click(object sender, EventArgs e)
        {
            tbStatus.Text = "Exit Entry Mode";
            PuzzleEdit = false;     // Done with editing blank puzzle
            PuzzleEditPanel.Visible = false;    // Make the Edit pannel invisible
            logList.Visible = true;     // Make the logList visible again
            if(LastSolution.X < 10)     // If there was a Last Solution make it white
                mySolutions[LastSolution.X, LastSolution.Y].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;

            if (Utilities.CheckForSolution() == false)  // No puzzle entered?  Error out
            {
                MessageBox.Show("No Puzzle Entered");
                LoadZPuzzle(puzzle);
                timer1.Start();
                return;
            }

            //            SolvePuzzleMethods((int)SolveMethods.SolveBacktracking, false);

            solution = puzzle;      // Otherwise solution may be null
            ResetPuzzle();              // Zero out the current puzzle and solution arrays
            SeeCandidates(true);        // Set the candidates vto visible
            int PuzzleLength = 0;       // Default length

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    string str = Rows[i, j].Text;   // Get the display text
                    if (str != "")                  // If not blank
                    {
                        int val = Utilities.GetDigit(str);  // What number is it?
                        ProcessRowEntry(i, j, val);         // Set the puzzle location to the number
                        puzzle[i, j] = val;
                        PuzzleLength++;         // Count number of Solutions
                    }
                }
            }

            if (PuzzleLength < 18)  // Not enough solution values in puzzle to insure solvability
            {
                var res = MessageBox.Show("There aren't enough numbers in the Puzzle. The Puzzle may not be solvable.  Do you want to continue?",
                "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.No)
                {
                    puzzle = (int[,])temppuz.Clone();
                    LoadZPuzzle(puzzle);    // Load the old puzzle and get out
                    timer1.Start();
                    return;
                }
            }
            // Do this so you'll have a solution for the new puzzle to which to refer
            SolvePuzzleMethods((int)SolveMethods.SolveBacktracking, false);  

        }   // End btnExit_Click

        private void NumBtn_Click(object sender, EventArgs e)
        {
            string mousebtn = ((MouseEventArgs)e).Button.ToString();
            tbStatus.Text = ("NBC1C: " + ((Label)sender).Name + "  " + mousebtn);

            if (LastSolution.X != 10)   // Check the last solution if there is one
            {
                string SName = ((Label)sender).Name;
                int num = Utilities.GetDigit((char)SName[2]);
                num++;

                mySolutions[LastSolution.X, LastSolution.Y].Text = num.ToString();
            }

        }   // End NumBtn_Click

        private void NumBtn_DoubleClick(object sender, EventArgs e)
        {
            string btn = ((MouseEventArgs)e).Button.ToString();
            tbStatus.Text = ("NBC2C: " + ((Label)sender).Name + "  " + btn);

        }   //  End NumBtn_DoubleClick

        private void Solution_Click(object sender, EventArgs e)
        {
            string mousebtn = ((MouseEventArgs)e).Button.ToString();
            tbStatus.Text = ("S1C: " + ((Label)sender).Name + "  " + mousebtn);

            SolutionClickHandler(sender, e, false);    // Flag is for double-click
        }   // End Solution_Click

        private void Solution_DoubleClick(object sender, EventArgs e)
        {
            string btn = ((MouseEventArgs)e).Button.ToString();
            tbStatus.Text = ("S2C: " + ((Label)sender).Name + "  " + btn);

            SolutionClickHandler(sender, e, true);  // Flag is for double-click
        }   //  End Solution_DoubleClick

        private void SolutionClickHandler(object sender, EventArgs e, bool DoubleClickFlag)
        {
            string mousebtn = ((MouseEventArgs)e).Button.ToString();
            string s;
            if (DoubleClickFlag)
                s = "DSCHandler1C: " + ((Label)sender).Name;
            else
                s = "SCHandler1C: " + ((Label)sender).Name;

            if (((Label)sender).Visible == true) { s += " Text: " + ((Label)sender).Text; }
            s += " " + mousebtn;
            tbStatus.Text = (s);

            string SName = ((Label)sender).Name;
            int box = Utilities.GetDigit((char)SName[1]);
            int sol = Utilities.GetDigit((char)SName[2]);

            if(PuzzleEdit)
            {
                if(LastSolution.X < 10)
                    mySolutions[LastSolution.X, LastSolution.Y].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;

                LastSolution.X = box;
                LastSolution.Y = sol;
                mySolutions[box, sol].BackColor = ColorDefs[(int)ColorsUsed.grnyel].Item1;
            }
            else
                GetCandidateAction( box, sol, mousebtn, DoubleClickFlag);

        }   // End SolutionClickHandler
         
        private void Candidates_Click(object sender, EventArgs e)
        {
            string nam = ((MouseEventArgs)e).Button.ToString();
            tbStatus.Text = ("C1C: " + ((Label)sender).Name + "  " + nam);
            CandidatesClickHandler(sender, e, false);   //   // Flag is for double-click
        }   // End Candidates_Click

        private void Candidates_DoubleClick(object sender, EventArgs e)
        {
            string nam = ((MouseEventArgs)e).Button.ToString();
            tbStatus.Text = ("C2C: " + ((Label)sender).Name + "  " + nam);
            CandidatesClickHandler(sender, e, true);   //   // Flag is for double-click

        }   //  End Solution_DoubleClick

        private void CandidatesClickHandler(object sender, EventArgs e, bool DoubleClickFlag)
        {
            string mousebtn = ((MouseEventArgs)e).Button.ToString();
            string parent = ((Label)sender).Parent.Name;
            string s = "CCHandler1C: " + ((Label)sender).Name
                     + " Parent: " + parent;
            if (((Label)sender).Visible == true) { s += " Text: " + ((Label)sender).Text; }
            s += " " + mousebtn;
            tbStatus.Text = (s);

            int box = Utilities.GetDigit((char)parent[1]);
            int sol = Utilities.GetDigit((char)parent[2]);

            GetCandidateAction(box, sol, mousebtn, DoubleClickFlag);

        }   // End SolutionClickHandler

        #endregion

        #region CellClickHandlers
        
        private void GetCandidateAction(int box, int sol, string mousebtn, bool DoubleClickFlag)
        {
            //  Sets the Solution color to LightCyan if there's Candidates and
            //  Processes NakedSingles if present.  Returns a list of Candidates
            //  visable in this Solution 
            List<int> LocalCandidateList = SetSolColor(box, sol);

            // Right mouse button clicked and more than 1 Candidate showing
            if ((mousebtn == "Right") && (LocalCandidateList.Count > 1))
            {
                // Show the form to Make or Exclude Candidates
                FrmSelectCandidate frmsc = new FrmSelectCandidate(LocalCandidateList);
                frmsc.ShowDialog();
                int make = frmsc.MakeItem;
                int exclude = frmsc.ExcludedItem;

                if (make > 0)
                {
                    Tuple<int, int> trow = Utilities.SolutionToRow(box, sol);
                    // Get the solution number for this puzzle location
                    int num = solution[trow.Item1, trow.Item2];

                    // This line hides the Candidates in this Solution
                    for (int k = 0; k < 9; k++) { Rows[trow.Item1, trow.Item2].Controls[k].Visible = false; }

                    mySolutions[box, sol].Text = make.ToString();

                    if ((make != num) && (PuzzleEdit == false))
                    {   // You picked the wrong number to make this cell, try again .
                        mySolutions[box, sol].BackColor = ColorDefs[(int)ColorsUsed.pnk].Item1;
                        tbStatus.Text = "Bad choice,  Try again.";
                        CandidateList = LocalCandidateList;
                    }
                    else
                    {
                        mySolutions[box, sol].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
                        ShedCandidates(trow.Item1, trow.Item2, make);
                        ResetFilters();
                    }
                }

                if (exclude > 0)
                {
                    if (MultiSelection.Count > 0)    // More than 1 selected
                    {
                        for (int i = 0; i < MultiSelection.Count; i++)  // Go through all
                        {
                            HandleExcludes(MultiSelection[i].Item1, MultiSelection[i].Item2, exclude, LocalCandidateList);
                        }
                        MultiSelection.RemoveRange(0, MultiSelection.Count);    // Empty out list
                        KeyShifted = false;     // Make sure this flag is reset
                    }
                    else
                    {
                        HandleExcludes(box, sol, exclude, LocalCandidateList);
                    }
                }
            }
            else if (mousebtn == "Left")
            {
                if (KeyShifted == true)
                {
                    MultiSelection.Add(Tuple.Create(box, sol));
                }
                else
                {
                    if (mySolutions[box, sol].BackColor == ColorDefs[(int)ColorsUsed.pnk].Item1)   // If a bad Solution was selected
                    {
                        Utilities.ResetSolution(box, sol);
                    }
                    else if ((PuzzleEdit == true) && (DoubleClickFlag))
                    {
                        Utilities.ResetSolution(box, sol);
                    }
                }
            }

            if (LastFilter >= 0)
                filterButton[LastFilter].BackColor = ColorDefs[(int)ColorsUsed.ltgrn].Item1;

        }   // End GetCandidateAction

        private void HandleExcludes(int box, int sol, int exclude, List<int> LocalCandidateList)
        {
            Tuple<int, int> trow = Utilities.SolutionToRow(box, sol); // Convert box,sol to row,col
            int win = solution[trow.Item1, trow.Item2];     // Get the solution for this cell
            if (exclude == win)     // If you're excluding the solution  - Error
            {
                mySolutions[box, sol].BackColor = ColorDefs[(int)ColorsUsed.pnk].Item1;
                tbStatus.Text = "Bad choice,  Try again.";
            }
            else
            {   // If OK, make candidate invisable and copy the candidate list over
                mySolutions[box, sol].Controls[exclude - 1].Visible = false;
                CandidateList = LocalCandidateList;
            }
        }   // End HandleExcludes

        private List<int> SetSolColor(int box, int sol)
        {
            List<int> CandidateShowingList = new List<int>();

            if (LastSolution.X != 10)   // Check the last solution if there is one
            {
                if (mySolutions[LastSolution.X, LastSolution.Y].Text == "") // If there is none
                    // Make solution white
                    mySolutions[LastSolution.X, LastSolution.Y].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
            }

            LastSolution.X = box;   // Save this as the "Last Solution" for next time
            LastSolution.Y = sol;

            if (mySolutions[box, sol].Text == "")   // No Solution displayed.  Procss Candidates
            {
                int seesol = 0;
                int cnt = 0;
                for (int i = 0; i < 9; i++)
                {   // Count up the number of Candidates showing
                    if (mySolutions[box, sol].Controls[i].Visible == true)
                    {
                        cnt++;
                        seesol = i;
                        CandidateShowingList.Add(i);
                    }
                }
                if (cnt == 1)   // Only 1 Candidate showing. Process as NakedSingle
                {
                    mySolutions[box, sol].Controls[seesol].Visible = false;
                    mySolutions[box, sol].Text = (seesol + 1).ToString();
                    mySolutions[box, sol].BackColor = ColorDefs[(int)ColorsUsed.ltyel].Item1;  // Light Yellow
                    // Get rid of the other Candidates of that number in its Box-Row-Col

                    Tuple<int, int> trow = Utilities.SolutionToRow(box, sol);
                    ShedCandidates(trow.Item1, trow.Item2, (seesol + 1));
                }
                else
                {   // More than 1 Candidate showing.  Turn Solution block yellow
                    mySolutions[box, sol].BackColor = ColorDefs[(int)ColorsUsed.yel].Item1;
                }
            }
            return CandidateShowingList;
        }   // End SetSolColor

        #endregion

        #region Array

        // I've got these all here because they're all used by the Array Methods
        public static Panel[] myBoxes = new Panel[9];
        public static Label[,] mySolutions = new Label[9, 9];
        public static Label[,] myCandidates = new Label[9, 9];
        public static Label[,] Rows = new Label[9, 9];
        public static Label[,] Columns = new Label[9, 9];
        public static Point[] BoxLoc = new Point[9];
        public static Point LastSolution = new Point(10, 10);
        public static bool CandidatesShowing = false;

        private void BoxesSetup()
        {
            int BoxSize = (MainPanel.Size.Width / 3);
            BoxSize -= offset;

            Utilities.CalcBoxPos(BoxSize, offset);
            for (int i = 0; i < 9; i++)
            {
                myBoxes[i] = new System.Windows.Forms.Panel()
                {
                    BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D,
                    Size = new System.Drawing.Size(BoxSize, BoxSize),
                    Location = BoxLoc[i],
                    Name = "B" + i.ToString()
                };

                Controls.Add(myBoxes[i]);
                myBoxes[i].SuspendLayout();
                MainPanel.Controls.Add(myBoxes[i]);
                myBoxes[i].ResumeLayout(false);

            }
        } //  End BoxesSetup            private System.Windows.Forms.Panel p2;

        private void SolutionsSetup()
        {
            int BoxSize = (myBoxes[0].Size.Width / 3) - 2;
            Font myFont = Utilities.TextSetup("Arial", ColorDefs[(int)ColorsUsed.blk].Item1, (BoxSize - offset), (BoxSize - offset));

            Utilities.CalcBoxPos(BoxSize, offset);

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    mySolutions[i, j] = new System.Windows.Forms.Label()
                    {
                        BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle,
                        Size = new System.Drawing.Size(BoxSize, BoxSize),
                        Location = BoxLoc[j],
                        Name = "S" + i.ToString() + j.ToString(),
                        Font = myFont,
                        TextAlign = ContentAlignment.MiddleCenter,
                        BackColor = ColorDefs[(int)ColorsUsed.wht].Item1,
                        Text = (j + 1).ToString()
                    };

                    Controls.Add(mySolutions[i, j]);
                    mySolutions[i, j].Click += new EventHandler(Solution_Click);
                    mySolutions[i, j].DoubleClick += new EventHandler(Solution_DoubleClick);
                    mySolutions[i, j].SuspendLayout();
                    myBoxes[i].Controls.Add(mySolutions[i, j]);
                    mySolutions[i, j].ResumeLayout(false);
                }
            }
            Utilities.MakeRows();
            Utilities.MakeColumns();
        } //  End SolutionsSetup

        private void CandidatesSetup()
        {
            int BoxSize = (mySolutions[0, 0].Size.Width / 3) - offset + 1;
            Font myFont = Utilities.TextSetup("Times New Roman", ColorDefs[(int)ColorsUsed.blk].Item1, BoxSize, BoxSize);
            Utilities.CalcBoxPos(BoxSize, offset);

            for (int k = 0; k < 9; k++)
            {
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        myCandidates[i, j] = new System.Windows.Forms.Label()
                        {
                            BorderStyle = System.Windows.Forms.BorderStyle.None,
                            Size = new System.Drawing.Size(BoxSize, BoxSize),
                            Location = BoxLoc[j],
                            Name = "C" + i.ToString() + j.ToString(),
                            Font = myFont,
                            TextAlign = ContentAlignment.MiddleCenter,
                            Text = (j + 1).ToString()
                        };
                        myCandidates[i, j].Font = new Font(myCandidates[i, j].Font, FontStyle.Bold);

                        Controls.Add(myCandidates[i, j]);
                        myCandidates[i, j].Click += new EventHandler(Candidates_Click);
                        myCandidates[i, j].DoubleClick += new EventHandler(Candidates_DoubleClick);
                        myCandidates[i, j].BringToFront();
                        myCandidates[i, j].SuspendLayout();
                        mySolutions[k, i].Controls.Add(myCandidates[i, j]);
                        myCandidates[i, j].ResumeLayout(false);
                    }
                }
            }
        } //  End CandidatesSetup

        static public void BlankSolutions()
        {
            for (int k = 0; k < 9; k++)
            {
                for (int i = 0; i < 9; i++)
                {
                    mySolutions[k, i].Text = "";
                    mySolutions[k, i].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
                }
            }
        }   //  End BlankSolutions
      
        static public void SeeCandidates(bool see)
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    for (int k = 0; k < 9; k++)
                    {
                        mySolutions[i,j].Controls[k].Visible = see;
                    }
                }
            }
        }   //  End SeeCandidates

        static public void ShedCandidates(int row, int col, int val)
        {
            // Calculate the number of the box
            int box = (col / 3) + ((row / 3) * 3);

            for (int i = 0; i < 9; i++)
            {
                myBoxes[box].Controls[i].Controls[val - 1].Visible = false;
                Rows[row, i].Controls[val - 1].Visible = false;
                Columns[col, i].Controls[val - 1].Visible = false;
            }
        }   // End ShedCandidates

        static public bool CheckBoxRowsCols()
        {   // Checks to make sure that the solution has the correct form
            string str = "";
            Label[,] src = new Label[9, 9];

            for (int k = 1; k < 4; k++)
            {
                switch (k)
                {
                    case 1:
                        src = mySolutions; str = "Box";
                        break;
                    case 2:
                        src = Rows; str = "Row";
                        break;
                    case 3:
                        src = Columns; str = "Column";
                        break;
                    default:
                        break;
                }   // End switch

                char[] sols = new char[9];

                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        if (src[i, j].Text.Length == 0)
                        {
                            MessageBox.Show($"There is a number missing from {str} {i + 1}.");
                            return false;
                        }

                        sols[j] = src[i, j].Text[0];
                    }   // End for j

                    bool ret = Utilities.CheckForDups(sols);

                    if (ret == false)
                    {
                        MessageBox.Show($"{str} {i + 1} has duplicate values.");
                        return false;
                    }
                }   // End for i
            }   // End for k

            return true;
        }   // End CheckBoxRowsCols

        #endregion

        #region ToolStripMenu

        private void FileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "&New":
                    BuildPuzzle(true);
                    break;
                case "&Open":
                    LoadPuzzle();
                    break;
                case "&Save":
                    SavePuzzle(false);
                    break;
                case "Save &As":
                    SavePuzzle(true);
                    break;
                case "&Exit":
                    Application.Exit();
                    break;
                default:
                    break;
            }
        }   // End FileToolStripMenuItem_Click

        private void EditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "&Undo":
                    break;
                case "&Redo":
                    break;
                case "E&dit Blank Puzzle":
                    EditPuzzle(true);
                    break;
                case "Edit &Current Puzzle":
                    EditPuzzle(false);
                    break;
                default:
                    break;
            }
        }   // End EditToolStripMenuItem_Click

        private void ViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "Filter Candidates":
                    break;
                case "&Clear Filter":
                    ClearFilters();
                    break;
                case "&Blue Marker":
                    SetMarker((int)ColorsUsed.dsblu);
                    break;
                case "&Green Marker":
                    SetMarker((int)ColorsUsed.spgrn);
                    break;
                case "&Pink Marker":
                    SetMarker((int)ColorsUsed.ltpnk);
                    break;
                case "&Amber Marker":
                    SetMarker((int)ColorsUsed.org);
                    break;
                case "&Remove Marker":
                    RemoveMarker(false);
                    break;
                case "Remove &All Markers":
                    RemoveMarker(true);
                    break;
                default:
                    break;
            }
        }   // End ViewToolStripMenuItem_Click

        private void ActionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "&Hints":
                    SolvePuzzleMethods((int)SolveMethods.Kermalis, false);  // Kermalis Lib - flag only used for DLM
                    break;
                case "Show &Next Step":
                    ShowNextStep();
                    break;
                case "&Solve All 'Naked Singles'":
                    Solvers.SolveNakedSingles();
                    ResetFilters();
                    break;
                case "&Generate Puzzle":
                    GeneratePuzzle();
                    break;
                case "Check &Puzzle":
                    CheckPuzzle(true);
                    break;
                case "See &Answers":
                    LoadZPuzzle(solution);
                    timer1.Stop();
                    break;
                case "&Reload Puzzle":
                    LoadZPuzzle(puzzle);
                    timer1.Start();
                    break;
                default:
                    break;
            }
        }   // End ActionsToolStripMenuItem_Click

        private void OptionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "Block Invalid Moves":
                    break;
                case "Show Candidates":
                    if (showCandidatesToolStripMenuItem.Checked)
                    {
                        if (CandidatesShowing == false)
                        {
                            LoadZPuzzle(puzzle);
                            CandidatesShowing = true;
                        }
                    }
                    else 
                    {
                        if (CandidatesShowing == true)
                        {
                            SeeCandidates(false);
                            CandidatesShowing = false;
                        }
                    }
                    break;
                case "Show Candidates While Filtering":
                    break;
                default:
                    break;
            }

        }   // End optionsToolStripMenuItem_Click

        private void HelpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "C&ontents":
                    string ack=  "Acknowledgements for the use of code found in GITHUB: " + Environment.NewLine +
                        "    Kermalis/SudokuSolver " + Environment.NewLine +
                        "    mf11y/SudokuSolver" + Environment.NewLine +
                        "    CidVonHighwind/SudokuSolver" + Environment.NewLine +
                        "    " + Environment.NewLine 
                        ;
                    MessageBox.Show(ack);
                    break;
                case "&About":
                    string vers = "The version of the currently executing assembly is: " + Application.ProductVersion 
                                    + Environment.NewLine +
                                    "This program was written in C# using MS Visual Studio 2019." + Environment.NewLine +
                                    "It uses .NET Framework 4.8 and was run and tested under Windows 10." +
                                    "    " + Environment.NewLine;
                    MessageBox.Show( vers);
                    break;
                case "&Copyright":
                    string str = "This program, \"YAS - Yet Another Sudoku\" " + Environment.NewLine +
                        "Copyright (C) 2020 by John Burke" + Environment.NewLine +
                        "under the GNU GENERAL PUBLIC LICENSE Version 3, 29 June 2007 " + Environment.NewLine +
                        "currently available at https://www.gnu.org/licenses/gpl-3.0.en.html";
                    MessageBox.Show(str);
                    break;
                default:
                    break;
            }
        }   // End HelpToolStripMenuItem_Click

        private void FilterCandidateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "Filter on 1's":
                    ActivateFilter(1);
                    break;
                case "Filter on 2's":
                    ActivateFilter(2);
                    break;
                case "Filter on 3's":
                    ActivateFilter(3);
                    break;
                case "Filter on 4's":
                    ActivateFilter(4);
                    break;
                case "Filter on 5's":
                    ActivateFilter(5);
                    break;
                case "Filter on 6's":
                    ActivateFilter(6);
                    break;
                case "Filter on 7's":
                    ActivateFilter(7);
                    break;
                case "Filter on 8's":
                    ActivateFilter(8);
                    break;
                case "Filter on 9's":
                    ActivateFilter(9);
                    break;
                case "Filter on Pairs":
                    ActivateFilter(10);
                    break;
                default:
                    break;
            }
        }   // End FilterCandidateToolStripMenuItem_Click

        private void SolvePuzzleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            switch (sender.ToString())
            {
                case "&Dancing Links":
                    SolvePuzzleMethods((int)SolveMethods.SolveDLM, true);    // DLM - flag = Show DLM Solution in Puzzle Display
                    break;
                case "&Backtracking":
                    SolvePuzzleMethods((int)SolveMethods.SolveBacktracking, true);    // - flag only used for DLM
                    break;
                case "Naked Singles":
                    SolvePuzzleMethods((int)SolveMethods.SolveNakedSingles, true);    // - flag only used for DLM
                    break;
                case "Hidden Singles":
                    SolvePuzzleMethods((int)SolveMethods.FindHiddenSingles, true);    // - flag only used for DLM
                    break;
                case "&Kermalis Lib":
                    SolvePuzzleMethods((int)SolveMethods.Kermalis, true);    // - flag only used for DLM
                    break;
                default:
                    break;
            }
        }   // End SolvePuzzleToolStripMenuItem_Click

        private void DifficultyLevelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hardToolStripMenuItem.Checked = false;      // Reset all these to start
            mediumToolStripMenuItem.Checked = false;
            easyToolStripMenuItem.Checked = false;
            samuraiToolStripMenuItem.Checked = false;

            switch (sender.ToString())
            {
                case "Hard":
                    hardToolStripMenuItem.Checked = true;
                    DifficultyIdx = (int)PuzzleGenerator.DiffDefsidx.hard;
                    tbDifficulty.Text = "Hard";
                    break;
                case "Medium":
                    mediumToolStripMenuItem.Checked = true;
                    DifficultyIdx = (int)PuzzleGenerator.DiffDefsidx.medium;
                    tbDifficulty.Text = "Medium";
                    break;
                case "Easy":
                    easyToolStripMenuItem.Checked = true;
                    DifficultyIdx = (int)PuzzleGenerator.DiffDefsidx.easy;
                    tbDifficulty.Text = "Easy";
                    break;
                case "Extreme":
                    samuraiToolStripMenuItem.Checked = true;
                    DifficultyIdx = (int)PuzzleGenerator.DiffDefsidx.extreme;
                    tbDifficulty.Text = "Extreme";
                    break;
                default:
                    break;
            }

            tbDifficulty.Text = sender.ToString();

        }   // End DifficultyLevelToolStripMenuItem_Click

        #endregion

        #region ToolStripHandlers

        void EditPuzzle( bool BlankFlag)
        {
            //            Array.Clear(puzzle,0,puzzle.Length);        // Zero the array entries
            //            Array.Clear(solution, 0, puzzle.Length);    // Zero the array entries

            logList.DataSource = null;
            logList.Items.Clear();
            logList.Visible = false;

            if (BlankFlag == true) 
            {
                temppuz = (int[,])puzzle.Clone();    // Hang on to this just in case
                BlankSolutions(); 
            }       // Just as the name implies
            SeeCandidates(false);
            PuzzleEdit = true;

            PuzzleEditPanel.Visible = true;

        }   // End EditPuzzle

        private void ShowNextStep()
        {
            // Get Display Puzzle into local array
            int[,] puz = puzzle;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    puz[i, j] = 0;

                    if (Rows[i, j].Text != "")
                    {
                        int x = Convert.ToInt32(Rows[i, j].Text);
                        if ((x > 0) && (x < 10))
                            puz[i, j] = x;
                    }
                }
            }

            int row = 0;
            int col = 0;
            int sol = 0;
            List<Tuple<int, int, int>> Hints = new List<Tuple<int, int, int>>();
            Tuple<int, int, int> tHint;

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    //                    if (puz[i, j] == 0)
                    if (puz[i, j] == 0)
                    {
                        tHint = new Tuple<int, int, int>(i, j, solution[i, j]);
                        Hints.Add(tHint);
                    }
                }
            }

            if (Hints.Count > 0)
            {
                int ifind = PuzzleGenerator.RandomGenerator(0, (Hints.Count - 1));
                row = Hints[ifind].Item1;
                col = Hints[ifind].Item2;
                sol = Hints[ifind].Item3;
                string str = "Row " + Alpha[row] + "  Column " + (col + 1).ToString() + "  = " + sol.ToString();
                tbStatus.Text = str;
            }
            else { tbStatus.Text = "No Next Steps available."; }

        }   // End ShowNextStep

        private void SolvePuzzleMethods(int i, bool flag)
        {   // - flag only used for DLM
            ResetTimer();
            timer1.Start(); // Show the Solve time

            HiddenSingles lclHS = new HiddenSingles();

            switch (i)
            {
                case (int)SolveMethods.SolveDLM:
                    solution = Solvers.SolveDLM(puzzle, flag);     // DLM - flag = Show DLM Solution in Puzzle Display
                    break;
                case (int)SolveMethods.SolveBacktracking:
                    solution = Solvers.SolveBacktracking(puzzle);             // Backtracking
                    if(solution == null)
                        MessageBox.Show("No solution was found.");
                    else
                    {
                        LoadZPuzzle(solution);
                        timer1.Stop();
                    }
                    break;
                case (int)SolveMethods.SolveNakedSingles:
                    Solvers.SolveNakedSingles();             // Naked Singles
                    break;
                case (int)SolveMethods.FindHiddenSingles:
                    lclHS.FindHiddenSingles(myBoxes, mySolutions);  // Hidden Singles
                    break;
                case (int)SolveMethods.Kermalis:
                    SetUpSolver(false);     // Kermalis - Custom = false - Show Solution (Hints) in logList
                    SolvePuzzle();
                    break;
                default:
                    break;
            }

            timer1.Stop();
            ResetFilters();
        }   // End SolvePuzzleMethods

        private void BtnAction_Click(object sender, EventArgs e)
        {
            int j = (int)((Button)sender).Tag;

            switch (j)
            {
                case (int)ActionButOrd.GenPuz:     // White grid with three grey cells
                    GeneratePuzzle();
                    break;
                case (int)ActionButOrd.PuzDif:     // Hammer
                    difficultyLeveltoolStripMenuItem.ShowDropDown(); 
                    break;
                case (int)ActionButOrd.ChkPuz:     // Checkmark
                    CheckPuzzle(true);
                    break;
                case (int)ActionButOrd.ShowStep:     // Questionmark
                    ShowNextStep();  // Show Next Step in the Status Box
                    break;
                case (int)ActionButOrd.NkdSing:     // White grid with black cell in the middle
                    SolvePuzzleMethods((int)SolveMethods.SolveNakedSingles, true);  // SolveNakedSingles - flag only used for DLM
                    break;
                case (int)ActionButOrd.HidSing:     // Brown grid with yellow cell in the middle
                    SolvePuzzleMethods((int)SolveMethods.FindHiddenSingles, true);  // SolveHiddenSingles - flag only used for DLM
                    break;
                case (int)ActionButOrd.DLM:     // Ballerina shoes
                    SolvePuzzleMethods((int)SolveMethods.SolveDLM, true);  // DLM - flag = Show DLM Solution in Puzzle Display
                    break;
                case (int)ActionButOrd.Kermalis:     // Pile of books
                    SolvePuzzleMethods((int)SolveMethods.Kermalis, true);    // Kermalis - Custom = false - Show Solution (Hints) in logList
                    break;                          // flag only used for DLM
                default:
                    break;
            }
        }   // End BtnAction_Click

        private void BtnFilter_Click(object sender, EventArgs e)
        {
            // Reset all the button colors to start
            for (int i = 0; i < filterButton.Length; i++)
            { filterButton[i].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1; }

            int j = (int)((Button)sender).Tag;
            LastFilter = j;
            if (j == 0) { ClearFilters(); }
            else
            {
                filterButton[j].BackColor = ColorDefs[(int)ColorsUsed.ltgrn].Item1;
                ActivateFilter(j);
            }
        }   // End BtnFilter_Click

        void ActivateFilter(int val)
        {
            if (filterButton[val].Text != "")
            {
                bool FilterOn = false;
                tbStatus.Text = (val.ToString());
                if (FilterActive > 0) { ClearFilters(); } // Clear any existing filters

                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        if (Rows[i, j].Text == "")        // If no Solution showiing
                        {
                            if (val == 10)   // Filter Pairs
                            {
                                int counter = 0;
                                for (int k = 0; k < 9; k++)
                                {
                                    if (Rows[i, j].Controls[k].Visible == true)
                                        counter++;
                                }
                                if (counter == 2)
                                {
                                    Rows[i, j].BackColor = ColorDefs[(int)ColorsUsed.ltgrn].Item1;
                                    FilterOn = true;
                                }
                            }
                            else    // Filter on val
                            {
                                if (Rows[i, j].Controls[val - 1].Visible == true)
                                {
                                    Rows[i, j].BackColor = ColorDefs[(int)ColorsUsed.ltgrn].Item1;
                                    FilterOn = true;
                                }
                            }
                        }   // End test Text
                    }   // End for j
                }   // End for i
                if (FilterOn == false)
                {
                    filterButton[val].BackColor = ColorDefs[(int)ColorsUsed.ltgray].Item1;
                    filterButton[val].Text = "";
                }
                else
                {
                    FilterActive = val;
                }
            }
        }   // End ActivateFilter

        void ResetFilterButtons()
        {
            for (int i = 1; i < 11; i++)
            {
                filterButton[i].Text = i.ToString();
                filterButton[i].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
            }
        }   // End ResetFilterButtons

        void ClearFilters()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (Rows[i, j].Text == "")
                    {
                        Rows[i, j].BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
                    }
                }   // End for j
            }   // End for i
            FilterActive = 0;
        }   // End ClearFilters

        #endregion

        #region Marker stuff
        public struct StMarkers 
        {
            public Point location;
            public Color[] box;
            public Color[] row;
            public Color[] column; 
        }
        List<StMarkers> MarkerList = new List<StMarkers>();

        void SetMarker( int MarkerColor )
        {
            // Save location, box/cell colors, row colors, column colors in list
            // make box/cell, row, column new color

            if (LastSolution.X < 10)
            {
                StMarkers Mark = new StMarkers();
                Mark.location = LastSolution;

                Color[] boxcolors = new Color[9];
                Color[] rowcolors = new Color[9];
                Color[] colcolors = new Color[9];
                Tuple<int, int> trow = Utilities.SolutionToRow(LastSolution.X, LastSolution.Y);
                int lrow = trow.Item1;
                int lcol = trow.Item2;

                for (int j = 0; j < 9; j++)
                {
                    boxcolors[j] = mySolutions[LastSolution.X, j].BackColor;
                    rowcolors[j] = Rows[lrow, j].BackColor;
                    colcolors[j] = Columns[lcol, j].BackColor;
                }
                Mark.box = boxcolors;
                Mark.row = rowcolors;
                Mark.column = colcolors;
                MarkerList.Add(Mark);

                for (int j = 0; j<9; j++)
                {
                    mySolutions[LastSolution.X, j].BackColor = ColorDefs[MarkerColor].Item1;
                    Rows[lrow, j].BackColor = ColorDefs[MarkerColor].Item1;
                    Columns[lcol, j].BackColor = ColorDefs[MarkerColor].Item1;
                }
            }
        }   // End SetMarker

        void RemoveMarker(bool AllFlag)
        {
            // Check if there are any markers
            // if AllFlag false: remove last marker
            //  if AllFlag true: remove all markers
            int cnt = MarkerList.Count;
            if (cnt > 0)
            {
                if(AllFlag == false)    // remove last marker
                {
                    ResetMarker(MarkerList[cnt - 1]);
                    MarkerList.RemoveAt(cnt - 1);
                }
                else     // remove all markers
                { 
                    for(int i=(cnt-1); i >= 0; i--)
                    {
                        ResetMarker(MarkerList[i]);
                        MarkerList.RemoveAt(i);
                    }
                }
            }
            else { MessageBox.Show("There are no markers to remove.");  }

        }   // End RemoveMarker

        void ResetMarker(StMarkers Mark)
        {
            Tuple<int, int> trow = Utilities.SolutionToRow(Mark.location.X, Mark.location.Y);
            int lrow = trow.Item1;
            int lcol = trow.Item2;

            for (int j = 0; j < 9; j++)
            {
                mySolutions[Mark.location.X, j].BackColor = Mark.box[j];
                Rows[lrow, j].BackColor = Mark.row[j];
                Columns[lcol, j].BackColor = Mark.column[j];
            }
        }   // End ResetMarker


        #endregion

        #region Action Methods

        static public void UpdateStatus()
        {
            int togo = 0;
            FrmYAS frm1 = new FrmYAS();

            frm1.tbStatus.BackColor = ColorDefs[(int)ColorsUsed.wht].Item1;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (mySolutions[i, j].Text == "")
                        togo++;
                }
            }

            int solved = 81 - togo;
            if(togo == 0)
            {
                StopTimer();
                if (CheckPuzzle(false) == true)
                    MessageBox.Show("Congratulations! The puzzle is solved and correct!");
            }
            frm1.tbStatus.Text = "Cells Solved = " + solved.ToString() + "   To Go = " + togo.ToString();
        }   // End UpdateStatus

        private void ResetPuzzle()
        {
            for(int i =0;  i<9;i++)
            {
                for(int j=0;j<9;j++)
                {
                    puzzle[i,j] = 0;
                    solution[i,j] = 0;
                }
            }

            seeAnswersToolStripMenuItem.Enabled = false;
            resetPuzzleToolStripMenuItem.Enabled = false;

        }   // End ResetPuzzle

        static public void LoadZPuzzle(int[,] puzzle)
        {
            BlankSolutions();       // Writes "" to each mySolution
            SeeCandidates(true);    // Makes mySolutions[k, i].Controls[j].Visible = true
            for (int i = 0; i < 9; i++) // File Line Number corresponds to Row number
            {
                for (int j = 0; j < 9; j++) // Column position in File Line Corresponds to Column in Row
                {
                    int ky = puzzle[i,j];
                    if (ky > 0)
                    {
                        ProcessRowEntry(i, j, ky);  // Sets up solution in a box and fixes box/row/column candidates
                    }
                }   // End for j
            }   // End for i

            UpdateStatus();
            CandidatesShowing = true;
        }   // End LoadZPuzzle

        private void LoadPuzzle()
        {
            var d = new OpenFileDialog  // Open new FileDialog control
            {
                Title = "Open Sudoku Puzzle",
                InitialDirectory = LastDir
            };

            if (d.ShowDialog() == DialogResult.OK)
            {
                BlankSolutions();
                SeeCandidates(true);
                int PuzzleLength = 0;
                
                string[] fileLines = File.ReadAllLines(d.FileName);   // Read all the lines from the puzzle file
                if (fileLines.Length == 1)  // All numbers in a single line.  No inter-number symbols.
                {
                    string line = fileLines[0];
                    if (line.Length < 81)     // As the error message says
                    {
                        MessageBox.Show($"Puzzle has 1 row but it is not at least 81 characters long.  It was only {line.Length} long.");
                        return;
                    }

                    ResetFilterButtons();   // Turn off the filters

                    ResetPuzzle();          // Set Puzzle values to 0

                    string fline = "";

                    for (int i = 0; i < line.Length; i++)   // Get rid of the separators (Tildes '~')
                    {
                        char achar = (char)line[i];
                        if (achar != '~')
                            fline += achar;
                    }

                    if (fline.Length != 81)     // As the error message says
                    {   
                        MessageBox.Show($"Puzzle has 1 row but must be 81 characters long after separators (Tildes '~') are removed.  It was {fline.Length} long.");
                        return;
                    }

                    for (int i = 0; i < 9; i++) // Use separate 'Row' and 'Col' values so the 
                    {                           // ProcessRowEntry() method can be used
                        for (int j = 0; j < 9; j++)
                        {
                            int ix = (i * 9) + j;
                            int ky = ((char)fline[ix]) - 48;    // Convert the char value to an int
                            if ((ky > 0) && (ky < 10))      // If the value is 1 - 9, store it; otherwise leave the '0' value
                            {
                                ProcessRowEntry(i, j, ky);
                                puzzle[i, j] = ky;
                                PuzzleLength++;
                            }
                        }
                    }
                }
                else if (fileLines.Length < 9)     // As the error message says
                {   // Not enough rows
                    MessageBox.Show($"A puzzle file must have either 1 row or at least 9 rows.  This one had {fileLines.Length}.");
                    return;
                }
                else
                {
                    ResetFilterButtons();   // Turn off the filters

                    ResetPuzzle();          // Set Puzzle values to 0

                    for (int i = 0; i < 9; i++) // File Line Number corresponds to Row number; only do 9 lines
                    {
                        string fline = "";
                        string line = fileLines[i];     // Process each line

                        for (int k = 0; k < line.Length; k++)      // Get rid of the separators (Tildes '~') 
                        {                           
                            char achar = (char)line[k];
                            if (achar != '~')
                                fline += achar;
                        }

                        if (fline.Length < 9)   // Not enough values
                        {
                            MessageBox.Show($"Row {i} must have at least 9 values after the separators (Tildes '~') are removed.");
                            return;
                        }

                        for (int j = 0; j < 9; j++) // Column position in File Line Corresponds to Column in Row
                        {
                            int ky = ((char)fline[j]) - 48;    // Convert the char value to an int

                            if ((ky > 0) && (ky < 10))      // If the value is 1 - 9, store it; otherwise leave the '0' value
                            {
                                ProcessRowEntry(i, j, ky);
                                puzzle[i, j] = ky;
                                PuzzleLength++;
                            }
                        }   // End for j
                    }   // End for i
                }

                LastDir = Path.GetDirectoryName(d.FileName);
                LastFileName = d.FileName;
                d.Dispose();
                //                Solvers lclSolvers = new Solvers();

                if (PuzzleLength == 81)
                {
                    MessageBox.Show("Puzzle values are full.  There is nothing left to solve");
                    return;
                }

                if (PuzzleLength < 18)
                {
                    var res = MessageBox.Show("There aren't enough numbers in the Puzzle. The Puzzle may not be solvable.  Do you want to continue?",
                        "",  MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if(res == DialogResult.No)
                        return;
                }
                
                solution = Solvers.SolveBacktracking(puzzle);   // Do this so you'll have a solution to which to refer
                UpdateStatus();

                ResetTimer();
                timer1.Start();
                if (solution == null)
                {
                    MessageBox.Show("The puzzle file has failed to load.");
                    return;
                }
                logList.DataSource = null;
                logList.Items.Clear();
                seeAnswersToolStripMenuItem.Enabled = true;
                resetPuzzleToolStripMenuItem.Enabled = true;
            }
        }   // End LoadPuzzle

        static public void ProcessRowEntry(int i, int j, int ky)
        {
            // This line hides the Candidates in this Solution
            for (int k = 0; k < 9; k++) { Rows[i, j].Controls[k].Visible = false; }

            Rows[i, j].Text = ky.ToString();     // Save to a Solution box
            Rows[i, j].Visible = true;
            Rows[i, j].BackColor = ColorDefs[(int)ColorsUsed.ltcyan].Item1;

            ShedCandidates(i, j, ky);
        }   // End ProcessRowEntry

        private void SavePuzzle(bool SaveAs)
        {
            string[] puz = new string[9];

            // Build a copy of the current puzzle in a row array
            for (int i=0; i<9;i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    Tuple<int, int> trow = Utilities.SolutionToRow(i, j);

                    if (mySolutions[i, j].Text != "")
                    {
                        puz[trow.Item1] += mySolutions[i, j].Text;
                    }
                    else
                    {
                        puz[trow.Item1] += "-";
                    }
                }   // End for j
            }   // End for i

            if (SaveAs == true)
            {
                var d = new SaveFileDialog  // Open new FileDialog control
                {
                    Title = "Save Sudoku Puzzle",
                    InitialDirectory = LastDir
                };

                DialogResult ret = d.ShowDialog();
                if (ret == DialogResult.OK)
                {
                    for (int i = 0; i < 9; i++)
                    {
                        File.WriteAllLines(d.FileName, puz);
                    }
                }
            }
            else 
            {
                if(LastFileName == "")
                {
                    MessageBox.Show("There is no File Name available. Try the 'SaveAs' Menu Fuction.");
                    return;
                }
                else
                    File.WriteAllLines(LastFileName, puz);
            }       
        }   // End SavePuzzle

        private void ProcessNewPuzzle()
        {
            int PuzzleLength = 0;

            for (int i = 0; i < 9; i++) // File Line Number corresponds to Row number
            {
                for (int j = 0; j < 9; j++) // Column position in File Line Corresponds to Column in Row
                {
                    if(Rows[i, j].Text != "")
                    {
                        int ky = Int32.Parse(Rows[i, j].Text);

                        if ((ky > 0) && (ky < 10))
                        {
                            ProcessRowEntry(i, j, ky);
                            puzzle[i, j] = ky;
                            PuzzleLength++;
                        }
                    }
                }   // End for j
            }   // End for i

            if (PuzzleLength < 18)
            {
                var res = MessageBox.Show("There aren't enough numbers in the Puzzle. The Puzzle may not be solvable.  Do you want to continue?",
                "", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.No)
                    return;
            }

            solution = Solvers.SolveBacktracking(puzzle);             // Backtracking
            if (solution == null)
            {
                MessageBox.Show("No solution was found.");
                return;
            }

        }   // End ProcessNewPuzzle

        static public bool CheckPuzzle(bool ShowMsg)
        {
            bool ret = CheckBoxRowsCols();
            if ((ret == true)&&(ShowMsg ==true))
            {
                MessageBox.Show("The puzzle checks OK.");
            }
            return ret;
        }   // End CheckPuzzle

        private void SetUpSolver(bool custom)
        {
            int[,] lclpuz = new int[9, 9];

            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    string str = Rows[i, j].Text;
                    if (str == "")
                    { lclpuz[i, j] = 0; }
                    else
                    { lclpuz[i, j] = Convert.ToInt32(str); }
                }
            }

            Utilities.Make_kPuzzle(lclpuz);       // Copy puzzle[,] to kPuzzle[][]
            solver = new Solver(new Puzzle(kPuzzle, custom));    // Make a new solver
            logList.DataSource = solver.Puzzle.Actions;
        }   // End SetUpSolver

        void SolvePuzzle()
        {
            // Clear solver's guesses on a custom puzzle
            if (solver.Puzzle.IsCustom)
            {
                for (int x = 0; x < 9; x++)
                {
                    for (int y = 0; y < 9; y++)
                    {
                        if (solver.Puzzle[x, y].Value != solver.Puzzle[x, y].OriginalValue)
                        {
                            solver.Puzzle[x, y].Set(0);
                        }
                    }
                }
            }
            stopwatch = new Stopwatch();
            var bw = new BackgroundWorker();
            bw.DoWork += solver.DoWork;
            bw.RunWorkerCompleted += SolverFinished;
            stopwatch.Start();
            bw.RunWorkerAsync();
        }   // End SolvePuzzle

        void SolverFinished(object sender, RunWorkerCompletedEventArgs e)
        {
            stopwatch.Stop();
            StatusPanel.Text = string.Format("Solver finished in {0} seconds.", stopwatch.Elapsed.TotalSeconds);
            solver.Puzzle.LogAction(string.Format("Solver {0} the puzzle", ((bool)e.Result) ? "completed" : "failed"));
            logList.SelectedIndex = solver.Puzzle.Actions.Count - 1;
            logList.Select();
        }   // End SolverFinished

        #endregion

        #region Generate Puzzle

        public void GeneratePuzzle()
        {
            /*
             *  This generates a new random solution. 
             *  Then it generates a puzzle from the solution.
             *  IT DOES NOT SOLVE A PUZZLE.
             */
            ResetFilterButtons();

            PuzzleGenerate();

            ClearFilters();

            logList.DataSource = null;
            logList.Items.Clear();
        }   // End GeneratePuzzle

        private void PuzzleGenerate()
        {
            tbStatus.Text = "";
            ResetTimer();
            StartTimer();

            PuzzleGenerator PG = new PuzzleGenerator();
            PG.DifficultyLevel = DifficultyIdx;
            PG.GeneratePuzzle();    // Generate a random solution then take out random locations to yield a puzzle
            puzzle = PG.PuzzleArray;    // Get the generated puzzle
            solution = PG.SolutionArray;    // Get the generated solution

            LoadZPuzzle(puzzle);    // Loads puzzle Rows into display
            seeAnswersToolStripMenuItem.Enabled = true;
            resetPuzzleToolStripMenuItem.Enabled = true;

            StopTimer();

            /* 
             * You'll want to generate Hints and NextSteps here          
             */

            if (GenFailed == false)
            {
                tbStatus.Text = "Gen Time - " + ShowElapsedTime(true);
                timer1.Start();
            }
            else
            {
                GenFailed = false;
                logList.DataSource = null;
                logList.Items.Clear();
            }
        }   // End PuzzleGenerate

        private void BuildPuzzle(bool DisplayPuzzle)
        {
            BlankSolutions();
            SeeCandidates(true);
            PuzzleEdit = true;
            if(DisplayPuzzle == true)
                GeneratePuzzle();

        }   // End BuildPuzzle

        #endregion

        #region Timer
        // These are defined here because they're all used by the timer methods
        int[] aTime = new int[4];   // Counter time
        int[] sTime = new int[4];   // Start time
        int[] eTime = new int[4];   // End time
        enum Tidx { ms, sec, min, hr };

        private void timer1_Tick(object sender, EventArgs e)
        {
            aTime[(int)Tidx.ms] += 1;
            if (aTime[(int)Tidx.ms] > 59) { aTime[(int)Tidx.ms] = 0; aTime[(int)Tidx.sec] += 1; }
            if (aTime[(int)Tidx.sec] > 59)
            { aTime[(int)Tidx.sec] = 0; aTime[(int)Tidx.min] += 1; }
            if (aTime[(int)Tidx.min] > 59) { aTime[(int)Tidx.min] = 0; aTime[(int)Tidx.hr] += 1; }
            tbTime.Text = aTime[(int)Tidx.hr].ToString("D2") + 
                ":" + aTime[(int)Tidx.min].ToString("D2") +
                ":" + aTime[(int)Tidx.sec].ToString("D2");
        }

        private void InitTimer()
        { 
            timer1.Interval = 10; 
            ResetTimer(); 
        }

        private void StartTimer()
        {
            for(int i = 0; i<4; i++) { sTime[i] = aTime[i]; }   // Reset Start time
            timer1.Start(); 
        }

        static public void StopTimer()
        {
            FrmYAS frm1 = new FrmYAS();

            frm1.timer1.Stop();
            for (int i = 0; i < 4; i++) { frm1.eTime[i] = frm1.aTime[i]; }   // Reset End time
        }

        private void ResetTimer()
        { 
            tbTime.Text = "00:00:00";   // Reset display time
            for (int i = 0; i < 4; i++) 
            {
                aTime[i] =0;
                sTime[i] = 0;
                eTime[i] = 0;
            }   // Reset times
        }

        private string ShowElapsedTime(bool ShowMS)
        {
            int[] lTime = GetElapsedTime();   // local time array

            string str = lTime[(int)Tidx.hr].ToString("D2") + ":" + lTime[(int)Tidx.min].ToString("D2") + ":" +
                lTime[(int)Tidx.sec].ToString("D2");

            if (ShowMS == true)
            {
                str += ":" + lTime[(int)Tidx.ms].ToString("D3");
            }
            return str;
        }

        private int[] GetElapsedTime()
        {
            int[] lTime = new int[4];   // local time array
            lTime[0] = lTime[1] = lTime[2] = lTime[3] = 0;
            long TotS = sTime[(int)Tidx.ms] + (sTime[(int)Tidx.sec] * 1000) + (sTime[(int)Tidx.min] * 60000)
                + (sTime[(int)Tidx.hr] * 3600000);
            long TotE = eTime[(int)Tidx.ms] + (eTime[(int)Tidx.sec] * 1000) + (eTime[(int)Tidx.min] * 60000)
                + (eTime[(int)Tidx.hr] * 3600000);
            long diff = TotE - TotS;
            long mod;
            if(diff > 0)
            {
                lTime[(int)Tidx.hr] = (int)(diff / (long)3600000);      // diff.hr = diff.tot / hr - ms
                mod = diff - ((lTime[(int)Tidx.hr] * (long)3600000));   // diff.mod.hr = diff.tot - (diff.hr * hr - ms)
                lTime[(int)Tidx.min] = (int)(mod / (long)60000);        // diff.min = diff.mod.hr / min - ms
                mod -= ((int)lTime[(int)Tidx.min] * (long)60000);       // diff.mod.min = diff.mod.hr - (diff.min * min - ms )
                lTime[(int)Tidx.sec] = (int)(mod / (long)1000);         // diff.sec = diff.mod.min / sec - ms
                mod -= ((int)(lTime[(int)Tidx.sec] * (long)1000));      // diff.mod.sec = diff.mod.min - (diff.sec * sec - ms)
                lTime[(int)Tidx.ms] = (int)(mod);                       // diff.ms = diff.mod.sec
            }
            return lTime;
        }

        #endregion

    }   // End class frmDLM : Form
}   // End namespace DLM
