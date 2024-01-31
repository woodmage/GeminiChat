using GenerativeAI.Methods;
using GenerativeAI.Models;
using GenerativeAI.Types;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Gemini
{
    public partial class GeminiChat : Form
    {
        private static readonly string version = "Gemini Chat version 0.0.2 alpha"; //version number
        private static readonly Dictionary<string, Action> Commands = new() { { "/quit", DoQuit }, { "/exit", DoQuit }, { "/help", DoHelp }, { "/new", DoNew }, { "/safe", DoSafe },
            { "/save", DoSave }, { "/load", DoLoad }, { "/reset", DoNew }, { "/params", DoParams }, { "/clear", DoClear }, { "/settings", DoSettings },
            { "/export", DoExport }, { "/import", DoImport }, { "/show", DoShow }, { "/api", DoAPIkey }, { "/version", DoVersion } }; //dictionary of Commands
        private static readonly Dictionary<string, string> Commanddescriptions = new() { { "/quit", "Quit the program" }, { "/exit", "Quit the program" }, { "/help", "Get command help" },
            { "/new", "Reset the conversation" }, { "/reset", "Reset the conversation" }, { "/safe", "Adjust the safety parameters" }, { "/save", "Save the conversation" },
            { "/load", "Load the conversation" }, { "/params", "Let you adjust the model parameters" }, { "/clear", "Clear the results area" },
            { "/settings", "Allow you to change the program settings" }, { "/pass", "Pass the following text without interpretation" }, { "/show", "Show the conversation" },
            { "/export", "Exports the conversation as a text file" }, { "/import", "Imports the conversation from a text file" }, { "/api", "Gets an api key" },
            { "/version", "Shows the version of this program" } }; //dictionary of Command descriptions
        private static readonly Dictionary<string, string> CommandParameters = new() { { "/save", "filename" }, { "/load", "filename" }, { "/import", "filename" },
            { "/export", "filename" }, { "/api", "api key" } }; //dictionary of Command Parameters that can be used
        private static readonly string savefile = "lastchat.conversation"; //filename for saving / restoring Chat session between program runs
        private static readonly string conversationfilter = "Conversation Files (*.conversation)|*.conversation"; //filter used for conversation files
        private static readonly string textfilefilter = "Text Files (*.txt)|*.txt"; //filter used for text files
        private static RichTextBox sresults = new(); //results control
        private static Button ssubmit = new(); //submit button
        private static TextBox squery = new(); //Query control
        private static Variables var = new(); //we won't need this until we are ready to save
        private static readonly Dictionary<int, HarmBlockThreshold> safetyconvert = new() {//a dictionary of HarmBlockThreshold values
            { 0, HarmBlockThreshold.BLOCK_NONE }, { 1, HarmBlockThreshold.BLOCK_ONLY_HIGH }, { 2, HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE },
            { 3, HarmBlockThreshold.BLOCK_LOW_AND_ABOVE } };
        private static readonly Dictionary<int, string> safetylevels = new() { //a dictionary of safety levels
            { 0, "Block None" }, { 1, "Block High" }, { 2, "Block Medium and Higher" }, { 3, "Block Low and Higher" } };
        private static Form safetyfrm = new(); //form used for safety settings dialog
        private static Label explainlb = new(), harasslbl = new(), sexuallbl = new(), dangerlbl = new(), hatesplbl = new(), explainha = new(), explainse = new(), 
            explainda = new(), explainhs = new(); //labels used for safety settings dialog
        private static NumericUpDown harassnud = new(), sexualnud = new(), dangernud = new(), hatespnud = new(); //numeric up down controls for safety settings dialog
        private static Button acceptbtn = new(), cancelbtn = new(); //buttons for safety settings dialog
        private static HarmBlockThreshold harass, sexual, danger, hatesp; //variables for safety settings dialog

        /// <summary>
        /// GeminiChat default constructor.
        /// </summary>
        public GeminiChat()
        {
            InitializeComponent(); //Windows requires this to be here.  It can be found in Form1.Designer.cs.
        }

        /// <summary>
        /// GeminiCreate deals with a null APIkey and returns a GeminiProModel object
        /// </summary>
        public static GeminiProModel GeminiCreate(string? APIkey)
        {
            APIkey ??= string.Empty; //if APIkey is null, make it an empty string
            return new(APIkey); //return the GeminiProModel object
        }

        /// <summary>
        /// DoAPIkey tries several different environment variables and even makes a form to get the user to enter an api key, then either redoes the Model and Chat session
        ///     or makes a error message and exits the program.
        /// </summary>
        private static void DoAPIkey()
        {
            Variables.APIkey = null; //set APIkey to null
            if (Variables.Param.Length > 0) //if we have a Parameter
            {
                Variables.APIkey = Variables.Param; //then let's try that as an api key
            }
            else //otherwise
            {
                Variables.APIkey ??= Environment.GetEnvironmentVariable("Gemini_API_Key", EnvironmentVariableTarget.User); //if APIkey is null, get one from "Gemini_API_Key"
                Variables.APIkey ??= Environment.GetEnvironmentVariable("Bard_API_Key", EnvironmentVariableTarget.User); //if APIkey is null, get one from "Bard_API_Key"
                Variables.APIkey ??= Environment.GetEnvironmentVariable("API_Key", EnvironmentVariableTarget.User); //if APIkey is null, get one from "API_Key"
                if (Variables.APIkey == null) //if APIkey is null
                {
                    Form form = new() //make a new form
                    {
                        ClientSize = new(380, 90), //it should have 380 x 90 client size
                        Text = "API Key" //give it a title
                    };
                    Label lbl = new() //make a new label
                    {
                        Location = new(10, 10), //place it at 10, 10
                        Size = new(150, 30), //it should be 150 x 30
                        Text = "An API key is needed.  Please enter it here:" //here's the text
                    };
                    TextBox tb = new() //make a new text box
                    {
                        Location = new(170, 10), //place it at 170, 10 (10 to the right of the label)
                        Size = new(200, 30), //it should be 200 x 30
                        Text = "", //it has no text to begin with
                        PlaceholderText = "Enter API key here." //use placeholder text to remind user what to enter here
                    };
                    Button btn = new() //make a new button
                    {
                        Location = new(160, 50), //place it at 160, 50 (in the middle of the line 10 below the previous line)
                        Size = new(60, 30), //it should be 60 x 30
                        Text = "Ok" //it should say "Ok"
                    };
                    form.Controls.Add(lbl); //add the label to the form
                    form.Controls.Add(tb); //add the textbox to the form
                    form.Controls.Add(btn); //add the button to the form
                    form.AcceptButton = btn; //set the form to use the button as its accept button
                    btn.Click += (s, e) => //define what to do if the button is clicked
                    {
                        Variables.APIkey = tb.Text; //set APIkey to the text in the textbox
                        form.Close(); //close the form
                    };
                    form.ShowDialog(); //show the form
                }
            }
            if (Variables.APIkey != null) //if we now have an api key
            {
                Variables.Build(true); //build a new model and chat in variables
            }
            else //otherwise (we've tried pretty much everything and still don't have an api key!)
            {
                MessageBox.Show("Sorry, an API key is required to run this program!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //give an error message
                Application.Exit(); //exit the program
            }
        }

        /// <summary>
        /// GC_Load called by the main window when it loads.
        ///    This method adjusts the window to fit the whole screen.
        /// </summary>
        private void GC_Load(object sender, EventArgs e)
        {
            if (Screen.PrimaryScreen != null) //if we have a primary screen
            {
                Size = Screen.PrimaryScreen.Bounds.Size; //use its boundaries to set the size of our app
                Location = new(0, 0); //put it in the upper left corner of the screen
            }
            squery.Dispose(); //remove Query control
            ssubmit.Dispose(); //remove submit button
            sresults.Dispose(); //remove results control
            Controls.Add(squery = new() //make a Query control and add it to the main window
            {
                AcceptsTab = true, //it accepts tab characters
                BackColor = Color.Black, //use black for the background
                ForeColor = Color.White, //use white for the foreground
                Font = new("Helvetica", 14), //use Helvetica at 14 points for the font
                Multiline = true, //make it multiline
                MaxLength = 131072, //set a very large maximum length (128K characters)
                PlaceholderText = "Enter your query or command here." //to help user with what to type here
            });
            squery.KeyDown += QueryKeyDown; //set a keydown handler for the query control
            Controls.Add(ssubmit = new() //make a submit button and add it to the main window
            {
                BackColor = Color.Black, //use black for the background
                ForeColor = Color.White, //use white for the foreground
                FlatStyle = FlatStyle.Flat, //use flat style
                Text = "Submit", //set text to Submit
                Font = new("MS Comic", 14) //use ms comic font at 15 points for the font
            });
            Controls.Add(sresults = new() //make a results control and add it to the main window
            {
                BackColor = Variables.BackColor, //use backcolor for the background
                Multiline = true, //make it multiline
                ReadOnly = true, //make it readonly
                ScrollBars = RichTextBoxScrollBars.Both, //set it to have both horizontal and vertical scrollbars
                TabStop = false //do not allow tabs to this control
            });
            ssubmit.Click += GC_Submit; //use GC_Submit to handle clicks
            AcceptButton = ssubmit; //use the submit button as the default accept button
            Variables.SizeReady = true; //and now we can allow a resize to occur
            ResizeIt(); //resize the window
            if (Variables.APIkey == null)
            {
                DoAPIkey(); //get an API key
            }
            if (File.Exists(AdjustPathname(savefile))) //if the file exists
            {
                DoLoad(savefile); //load the save file
            }
            DoVersion(); //display version information
        }

        private void QueryKeyDown(object? sender, KeyEventArgs e)
        {
            int firstcharseen = sresults.GetCharIndexFromPosition(new Point(0, 0)); //get the index of the first visible character
            int lastcharseen = sresults.GetCharIndexFromPosition(new Point(0, sresults.ClientSize.Height - 1)); //get the index of the last visible character
            int firstlineseen = sresults.GetLineFromCharIndex(firstcharseen); //get first line seen
            int lastlineseen = sresults.GetLineFromCharIndex(lastcharseen); //get last line seen
            int linesseen = lastlineseen - firstlineseen + 1; //get the number of visible lines
            if (e.Control) //if control key (ctrl) is held down
            {
                switch (e.KeyCode) //switch according to key code
                {
                    case Keys.Home: //if Home key
                        sresults.SelectionStart = 0; //move selection to the beginning
                        break; //done with Home key
                    case Keys.End: //if End key
                        sresults.SelectionStart = sresults.TextLength; //move selection to the end
                        break; //done with End key
                    case Keys.PageDown: //if PgDn key
                        sresults.SelectionStart = sresults.GetFirstCharIndexFromLine(Math.Max(0, firstlineseen + linesseen / 2)); //move selection half a page down
                        break; //done with PgDn key
                    case Keys.PageUp: //if PgUp key
                        sresults.SelectionStart = sresults.GetFirstCharIndexFromLine(Math.Max(0, firstlineseen - linesseen / 2)); //move selection half a page up
                        break; //done with PgUp key
                    default: //and other key
                        return; //let the other keys work as normal
                }
                sresults.ScrollToCaret(); //scroll to there
                sresults.SelectionStart = sresults.TextLength; //put the actual position at the end
                e.Handled = true; //tell windows we handled these keys
            }
        }

        /// <summary>
        /// DoVersion outputs the version information.
        /// </summary>
        public static void DoVersion()
        {
            Output(Variables.CodeFont, Variables.CodeColor, $"\n\n{version}"); //display version information
        }

        /// <summary>
        /// ResizeIt method resizes the components of the window to fit properly.
        /// </summary>
        private void ResizeIt()
        {
            Variables.SizeReady = false; //no resizes during the resize!
            squery.Location = new(10, 10); //set location of Query control
            squery.Size = new(ClientSize.Width - 110, 80); //set size of Query control
            ssubmit.Location = new(ClientSize.Width - 90, 10); //set location of submit control
            ssubmit.Size = new(80, 80); //set size of submit control
            sresults.Location = new(10, 100); //set location of result control
            sresults.Size = new(ClientSize.Width - 20, ClientSize.Height - 110); //set size of result control
            Variables.ClientSize = ClientSize; //make a static variable for ClientSize
            Variables.SizeReady = true; //and allow resizes again
        }

        /// <summary>
        /// GC_Resize is called by the main window when it is resized.
        ///     It makes sure it is alright to do a resize before calling ResizeIt to do the work.
        /// </summary>
        private void GC_Resize(object sender, EventArgs e)
        {
            if (Variables.SizeReady) //if we are ready to do a resize
            {
                ResizeIt(); //resize the window
            }
        }

        /// <summary>
        /// AdjustSubmit sets the text of the submit control and enables or disables the submit and Query controls
        /// </summary>
        /// <Param name="enabled"></Param> Whether to enable or disable the controls
        /// <Param name="title"></Param> The new title to be shown as the text of the submit control
        private static void AdjustSubmit(bool enabled, string title)
        {
            ssubmit.Text = title; //set text of submit control to title
            EnableControls(enabled); //enable or disable controls
        }

        /// <summary>
        /// EnableControls is a convenience method which enables or disables the submit and Query controls according to the value of enabled.
        /// </summary>
        /// <Param name="enabled"></Param> A boolean value: true enables the controls, false disables them.
        private static void EnableControls(bool enabled)
        {
            ssubmit.Enabled = squery.Enabled = enabled; //Enable or disable submit and Query controls
        }

        /// <summary>
        /// GC_Submit is called by the submit control when it is clicked.
        ///     It calls ParseCommand and handles any Commands.
        ///     When there is not a Command, it displays the Query and the response in the results control.
        /// </summary>
        private async void GC_Submit(object? sender, EventArgs e)
        {
            //Model.Config = 
            Variables.Query = squery.Text; //get the Query or Command
            if (!ModifierKeys.HasFlag(Keys.Shift)) //if we weren't holding the shift key (like shift-Enter)
            {
                if (ParseCommand() != null) //if it is a Command
                {
                    Output(Variables.UserFont, Variables.UserColor, Variables.HelpFont, Variables.HelpColor2, $"\n\nCommand \"`{Variables.Command}`\" running.");
                    Commands[Variables.Command](); //run the Command
                }
                else //otherwise (it is not a Command)
                {
                    AdjustSubmit(false, "Working"); //turn off submit and Query controls
                    string? result = null; //variable for result
                    try //try to get the result
                    {
                        result = await Variables.Chat.SendMessageAsync(Variables.Query); //get result from the Chat
                    }
                    catch (Exception ex)//if there was an error
                    {
                        string errormsg = $"There was an error.  Make sure you are online and if needed, do a \"/api\" Command.  The reported error is {ex.Message}"; //make error msg
                        Output(Variables.DebugFont, Variables.DebugColor, $"\n\n{errormsg}"); //show an error message
                        MessageBox.Show(errormsg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //show an error message
                    }
                    AdjustSubmit(true, "Submit"); //turn on submit and Query controls again
                    if (Variables.ClearFirst) //if we should clear the results control
                    {
                        sresults.Clear(); //clear the result control
                    }
                    Output(Variables.UserFont, Variables.UserColor, Variables.CodeFont, Variables.CodeColor, $"\n\n{Variables.Query}"); //display the user Query
                    int pos = sresults.TextLength; //get position
                    if (result != null) //if result from the Chat exists
                    {
                        Output(Variables.ReplyFont, Variables.ReplyColor, Variables.CodeFont, Variables.CodeColor, $"\n\n{result}"); //display the Model's reply
                    }
                    else
                    {
                        Output(Variables.DebugFont, Variables.DebugColor, "\n\nThe model did not return a response."); //report the lack of a response
                    }
                    sresults.SelectionStart = pos; //move selection back to position right after user query
                    sresults.ScrollToCaret(); //scroll there
                    sresults.SelectionStart = sresults.TextLength; //put selection back at the end
                }
                squery.Clear(); //clear the Query control
                squery.Focus(); //give the Query control focus
            }
            else //otherwise (we did a shift-Enter for a new line)
            {
                squery.AppendText(Environment.NewLine); //add a new line to the Query control
            }
        }

        /// <summary>
        /// ScrollToEnd simply scrolls to the end of the text in the results control.
        /// </summary>
        private static void ScrollToEnd()
        {
            sresults.SelectionStart = sresults.Text.Length; //set selection to be at end of text
            sresults.ScrollToCaret(); //scroll there
        }

        /// <summary>
        /// MakeSure displays a message box with the supplied prompt and a yes button and a no button.
        ///     It returns true if the user selects the yes button and false if the user selects the no button.
        /// </summary>
        /// <Param name="prompt"></Param> This is the prompt you wish to ask user.
        /// <returns>bool</returns> A boolean value: true for yes button selected, false otherwise.
        private static bool MakeSure(string prompt)
        {
            return MessageBox.Show($"{prompt}: Sure?", "Verify", MessageBoxButtons.YesNo) == DialogResult.Yes; //return true if user hit Yes
        }

        /// <summary>
        /// IsCommand checks the specified query string and returns true if is is in the Command dictionary.
        /// </summary>
        /// <Param name="query"></Param> This is the query that will be checked.
        /// <returns>bool</returns> Boolean: true if query is in Command dictionary, false otherwise.
        private static bool IsCommand(string query)
        {
            return Commands.ContainsKey(query.ToLower()); //return true if Command is in dictionary
        }

        /// <summary>
        /// ParseCommand takes the Query and checks for it being a Command.
        ///     If it is a Command, it sets the Command and Param variables and returns the Command.
        ///     If Command is pass, it handles it by setting the Query to the Param variable.
        ///     Otherwise, it returns null to show no Command.
        /// </summary>
        /// <returns>string?</returns> It returns either the Command or null to show no Command.
        private static string? ParseCommand()
        {
            Match match = CommandRegex().Match(Variables.Query); //get a match from Query for Command
            if (match.Success) //if we are successful
            {
                Variables.Command = $"/{match.Groups[1].Value}".ToLower(); //get value of Command and make it lower case
                Variables.Param = match.Groups[2].Value; //get Param(s) of Command
                if (Variables.Command == "/pass") //pass Command is a special case that needs to be handled here
                {
                    Variables.Query = Variables.Param; //set Query to Parameters of /pass Command
                    return null; //return null (as in there was no Command)
                }
                if (IsCommand(Variables.Command)) //if Command is in dictionary
                {
                    return Variables.Command; //return Command
                }
            }
            return null; //otherwise (not a match for a Command or not in dictionary), return null
        }

        /// <summary>
        /// DoQuit checks to make sure the user agrees before exiting the program
        /// </summary>
        public static void DoQuit()
        {
            if (MakeSure("Quit the program")) //if the user agrees
            {
                DoSave(savefile); //and save the Chat before exiting
                Application.Exit(); //exit the program
            }
        }

        /// <summary>
        /// Output is a helper function.  It sets the font and color and outputs the text in output
        /// </summary>
        /// <Param name="font"></Param> The font to be used
        /// <Param name="color"></Param> The color to be used
        /// <Param name="output"></Param> The text to output
        private static void Output(Font font, Color color, string? output)
        {
            sresults.SelectionFont = font; //set font to use
            sresults.SelectionColor = color; //set color to use
            if (output != null) //if output exists
            {
                sresults.AppendText(output); //handle output
                //ScrollToEnd(); //scroll to end
            }
        }

        /// <summary>
        /// Output is a helper function.  It sets the font and color according to outfont and outcolor and displays the text in output.
        ///     However, portions of output inside backticks are displayed using codefont and codecolor.
        ///     Blocks of output between triple backticks are displayed using codefont and codecolor and any text beside the triple backticks is treated as a label.
        /// </summary>
        /// <param name="outfont"></param> This is the normal output font used.
        /// <param name="outcolor"></param> This is the normal output color used.
        /// <param name="codefont"></param> This is the code output font used.
        /// <param name="codecolor"></param> This is the code output color used.
        /// <param name="output"></param> This is the output text used.
        private static void Output(Font outfont, Color outcolor, Font codefont, Color codecolor, string? output)
        {
            if (output == null || output.Length < 1) //if we have no output
            {
                return; //exit stage left!
            }
            List<(string, bool)> firstpass = []; //variable for result of first pass
            List<(string, bool)> finalresults = []; //variable for actual lines
            string[] lines = output.Split('\n'); //split the text into lines
            bool incodeblock = false; //boolean for in code block
            foreach (string line in lines) //for each line
            {
                if (line.StartsWith("```")) //if line starts with three backticks
                {
                    incodeblock = !incodeblock; //toggle the in code block flag
                    if (line.Length > 3) //if there is more than the three backticks on the line
                    {
                        string text = $"//[[{line.Trim('`')}]]"; //make a title string
                        firstpass.Add((text, true)); //add it as code
                    }
                    continue; //go to the next line
                }
                firstpass.Add((line, incodeblock)); // Add the line to the result of first pass
            }
            foreach (var linres in firstpass) //for each line in the result of first pass
            {
                string line = $"{linres.Item1}\n"; //set the line plus a carriage return
                bool iscode = linres.Item2; //set iscode variable
                if (line.Contains('`') && iscode == false) //if line contains backtick and we are not in a code block
                {
                    int startindex = 0; //variable for start index
                    int codeindex = line.IndexOf('`'); //variable for code indicator index
                    while (codeindex != -1) //while we have a code index
                    {
                        if (codeindex > startindex) //if the code index is past the start index
                        {
                            string text = line[startindex..codeindex]; //get the text before the code
                            finalresults.Add((text, false)); //add the text before the code
                        }
                        if (codeindex > 0 && line[codeindex - 1] == '\\') //check for escaped backtick
                        {
                            line = line.Remove(codeindex - 1, 1); //remove the backslash from the line
                            codeindex = line.IndexOf('`', codeindex); //find the next code occurrence
                            continue; //continue looping
                        }
                        int endindex = line.IndexOf('`', codeindex + 1, line.Length - codeindex - 1); // Find the matching closing backtick
                        if (endindex == -1) //if we didn't find it
                        {
                            string code = line[(codeindex + 1)..]; //get the remaining text
                            finalresults.Add((code, true)); //add it as code
                            codeindex = -1; //reset code index to break the loop
                            startindex = line.Length; //and change the start index as well
                            break; //and we are done
                        }
                        string codeText = line.Substring(codeindex + 1, endindex - codeindex - 1); //extract the code
                        finalresults.Add((codeText, true)); //add it as code
                        startindex = endindex + 1; //update the start index for the next iteration
                        codeindex = line.IndexOf('`', startindex); //find the next code occurrence
                    }
                    if (startindex < line.Length) //if there is more text
                    {
                        string remainingText = line[startindex..]; //extract the remaining text
                        finalresults.Add((remainingText, false)); //add it as normal text
                    }
                }
                else //otherwise
                {
                    finalresults.Add((line, iscode)); //just add the line
                }
            }
            foreach (var line in finalresults) //and now we actually process the lines - for each line
            {
                if (line.Item2) //if it is marked as code
                {
                    Output(codefont, codecolor, line.Item1); //output as code
                }
                else //otherwise (not marked as code)
                {
                    Output(outfont, outcolor, line.Item1); //output as normal
                }
            }
        }

        /// <summary>
        /// DoHelp sets up a help message and displays it in the results control.
        /// </summary>
        public static void DoHelp()
        {
            if (Variables.ClearFirst) //if we should clear the results area before output
            {
                sresults.Clear(); //clear the results control
            }
            Output(Variables.HelpFont, Variables.HelpColor, "\n\nHelp!\n\n"); //display help
            Output(Variables.HelpFont, Variables.HelpColor, "The upper left text input area is for your queries and Commands.\n"); //display help
            Output(Variables.HelpFont, Variables.HelpColor, "The button to the right of it will submit whatever you entered.\n"); //display help
            Output(Variables.HelpFont, Variables.HelpColor, "You will see the results in the large box on the bottom.\n\n"); //display help
            Output(Variables.HelpFont, Variables.HelpColor, "Here are your Commands available:"); //display help
            foreach ((string Command, string description) in Commanddescriptions) //for each dictionary entry in Command descriptions
            {
                Output(Variables.HelpFont, Variables.HelpColor, Variables.HelpFont, Variables.HelpColor2, $"\n`{Command}`: {description}"); //output command and the description
                if (CommandParameters.TryGetValue(Command, out string? value)) //if the Command is in the Command Parameter dictionary
                {
                    Output(Variables.HelpFont, Variables.HelpColor, Variables.HelpFont, Variables.HelpColor2, $"  Parameter: `{value}`"); //output the Parameter to the Command
                }
            }
            Output(Variables.HelpFont, Variables.HelpColor, "\n\nAs you can see, all Commands begin with the forward slash.\n"); //display help
            Output(Variables.HelpFont, Variables.HelpColor, "Note that some Commands may not yet be implemented.\n"); //display help
        }

        /// <summary>
        /// DoNew clears the Chat history.
        /// </summary>
        private static void DoNew()
        {
            Variables.Chat.History.Clear(); //clear the Chat history
        }

        /// <summary>
        /// DoSave saves the Chat history to a file.
        /// </summary>
        private static void DoSave()
        {
            string fname = GetSaveFile(conversationfilter); //get a save file
            if (fname.Length > 0) //if there is a save file
            {
                string json = JsonSerializer.Serialize(Variables.Chat.History); //serialize the Chat history to a string
                File.WriteAllText(fname, json); //write the file using the string
            }
        }

        /// <summary>
        /// DoSave with the filename Parameter saves the program state to the specified file.
        /// </summary>
        /// <Param name="filename"></Param> //The specified file name.
        private static void DoSave(string filename)
        {
            Variables.History = Variables.Chat.History; //make a copy of the chat history for serialization
            string fname = AdjustPathname(filename); //adjust the pathname for filename
            var.Save(fname); //save the program state to the file
        }

        /// <summary>
        /// DoLoad loads the Chat history from a file.
        /// </summary>
        private static void DoLoad()
        {
            string fname = GetLoadFile(conversationfilter); //get a file to load
            if (fname.Length > 0) //if there is a load file
            {
                if (File.Exists(fname)) //if the file exists
                {
                    string json = File.ReadAllText(fname); //read the file into the string json
                    List<Content>? newhistory = JsonSerializer.Deserialize<List<Content>>(json); //deserialize the json into a new history
                    if (newhistory != null) //if we got a history out of that
                    {
                        Variables.Chat.History.Clear(); //clear the history of the current Chat
                        Variables.Chat.History.AddRange(newhistory); //add the new history to our Chat history
                    }
                }
            }
            sresults.BackColor = Variables.BackColor; //set the background color for the results control
            DoShow(); //display the conversation
        }

        /// <summary>
        /// DoLoad with the filename Parameter loads the specified file then deserializes it to a new instance of Variables and displays the conversation, or displays an error message.
        /// </summary>
        /// <Param name="filename"></Param> The specified file name.
        private static void DoLoad(string filename)
        {
            string fname = AdjustPathname(filename); //adjust the pathname for filename
            Variables? vartemp = Variables.Load(fname); //load the program state from the file
            if (vartemp != null) //if we got one
            {
                var = vartemp; //an unnecessary assignment
                Variables.Build(false); //build the model and chat session using the retrieved history
                DoShow(); //display the conversation
            }
            else //else (there was an error in deserialization)
            {
                Output(Variables.DebugFont, Variables.DebugColor, $"\n\nError reading program state from {filename}."); //inform user about the error
            }
        }

        /// <summary>
        /// GetSaveFile creates a SaveFileDialog using the specified filter and returns the file specified by the user.
        /// </summary>
        /// <Param name="filter"></Param> //the filter to be used
        /// <returns></returns> //if the user selected a file, return the filename (with path), otherwise return an empty string
        private static string GetSaveFile(string filter)
        {
            if (Variables.Param.Length > 0) //if there is a Parameter specified
            {
                string[] split = filter.Split("|*.");//'|'); //this should get the default extension in the last part
                if (string.Compare(split[^1], Path.GetExtension(Variables.Param), true) != 0) //if the Parameter does not have the default extension
                {
                    Variables.Param = Path.ChangeExtension(Variables.Param, split[^1]); //give the default extension to the Parameter
                }
                return AdjustPathname(Variables.Param); //otherwise just return the Parameter (no .conv.conv here!)
            }
            else //otherwise we will have to ask the user to select a file
            {
                SaveFileDialog savedialog = new() //create a SaveFileDialog
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory, //use program's directory as the initial directory
                    Filter = filter //use filter as passed to this method
                };
                DialogResult result = savedialog.ShowDialog(); //show the dialog
                return result == DialogResult.OK ? savedialog.FileName : string.Empty; //return selected file if one was selected, otherwise empty string
            }
        }

        /// <summary>
        /// GetLoadFile will return a Parameter if present, otherwise it creates an OpenTextFile dialog and returns whatever the user selects.
        /// </summary>
        /// <Param name="filter"></Param>  This is the filter it uses.
        /// <returns>string</returns> It returns a string, either the selected file or an empty string if the user does not select one.
        private static string GetLoadFile(string filter)
        {
            if (Variables.Param.Length > 0) //if there is a Parameter specified
            {
                return AdjustPathname(Variables.Param); //return the Parameter
            }
            else //otherwise we will have to ask the user to select a file
            {
                OpenFileDialog opendialog = new()
                {
                    InitialDirectory = AppDomain.CurrentDomain.BaseDirectory, //use program's directory as the initial directory
                    Filter = filter //use filter as passed to this method
                }; //create an OpenFileDialog
                DialogResult result = opendialog.ShowDialog(); //show the dialog
                if (result == DialogResult.OK) //if the user selected a file
                {
                    return opendialog.FileName; //return the filename
                }
                return string.Empty; //otherwise return an empty string
            }
        }

        /// <summary>
        /// AdjustPathname is a convenience method that checks a filename for a path and if it doesn't have one, gives it the program's directory as its path.
        /// </summary>
        /// <Param name="filename"></Param> This is the filename to be tested.
        /// <returns>string</returns> The complete pathname.
        private static string AdjustPathname(string filename)
        {
            if (filename.Length > 0) //if the filename exists
            {
                if (!filename.Contains('\\')) //if it doesn't contain a backslash (it has no directory information)
                {
                    return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename); //return (programdirectory\filename)
                }
            }
            return filename; //return filename
        }

        /// <summary>
        /// DoShow displays the conversation from the Chat history
        /// </summary>
        private static void DoShow()
        {
            sresults.BackColor = Variables.BackColor; //set the background color
            DoClear(); //clear the results control
            foreach (var message in Variables.Chat.History) //for each message in Chat history
            {
                if (message.Role != null && message.Parts != null) //if we have a role and parts for this message
                {
                    string formattedmessage = $"\n\n{message.Parts[0].Text}";
                    switch (message.Role.ToLower()) //switch by the lower cased role
                    {
                        case "user": //if it is a user message
                            Output(Variables.UserFont, Variables.UserColor, Variables.CodeFont, Variables.CodeColor, formattedmessage); //display it
                            break;
                        case "model": //if it is a model message
                            Output(Variables.ReplyFont, Variables.ReplyColor, Variables.CodeFont, Variables.CodeColor, formattedmessage); //display it
                            break;
                        default: //if it is anything else
                            Output(Variables.DebugFont, Variables.DebugColor, Variables.CodeFont, Variables.CodeColor, formattedmessage); //display it
                            break;
                    }
                }
            }
            ScrollToEnd(); //scroll to end of results
        }

        /// <summary>
        /// DoExport reads through the Chat history and saves it to a text file
        /// </summary>
        private static void DoExport()
        {
            string fname = GetSaveFile(textfilefilter); //get a filename
            if (fname.Length > 0) //if we got a filename
            {
                using StreamWriter sw = new(fname); //write to the file
                {
                    foreach (var message in Variables.Chat.History) //for each message in Chat history
                    {
                        if (message.Parts != null) //if there is at least one message part
                        {
                            sw.WriteLine($"{message.Role}: {message.Parts[0].Text}"); //write the line
                        }
                    }
                }
            }
        }

        /// <summary>
        /// DoClear clears the results control.
        /// </summary>
        private static void DoClear()
        {
            sresults.Clear(); //clear the results control
        }

        /// <summary>
        /// NotYetImplemented shows an error message when you attempt to do a function that is not yet implemented.
        /// </summary>
        private static void NotYetImplemented()
        {
            MessageBox.Show("This function is not yet implemented.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); //show error message
        }

        /// <summary>
        /// DoImport reads a user specified text file and uses it to create a new Chat history which it also displays.
        /// </summary>
        private static void DoImport()
        {
            string fname = GetLoadFile(textfilefilter); //get a file name to use for importing
            if (fname.Length > 0) //if we got a file name
            {
                using StreamReader sr = new(fname); //read from the selected file
                {
                    DoNew(); //clear the current Chat history
                    DoClear(); //clear the results control
                    string lastrole = "user"; //variable for last role
                    string messageline = ""; //variable for message line
                    while (!sr.EndOfStream) //while we still have stuff to read
                    {
                        string? line = sr.ReadLine() + "\n"; //read in a line of text and add the newline which was stripped by ReadLine()
                        if (line == null) //if we didn't get anything
                        {
                            continue; //continue processing, I suppose
                        }
                        string[] lineparts = line.Split(':'); //split the line into the role and the message
                        if (lineparts.Length == 2) //if it was a Model: content type line
                        {
                            lastrole = lineparts[0].Trim(); //set the last role
                            messageline = lineparts[1].Trim(); //set the message line
                        }
                        else //otherwise (it was a noraml text line)
                        {
                            messageline = line; //set the message line
                        }
                        Content message = new()
                        {
                            Role = lastrole.Trim(), //set the role of the message
                            Parts = [new Part() { Text = messageline.Trim() }]
                        }; //make a new message
                        Variables.Chat.History.Add(message); //add the message to the Chat history
                        if (lastrole == "user") //if it was a user message
                        {
                            Output(Variables.UserFont, Variables.UserColor, Variables.CodeFont, Variables.CodeColor, $"\n\n{messageline.Trim()}\n\n"); //display it with the user's font and color
                        }
                        else //otherwise (Model's message)
                        {
                            Output(Variables.ReplyFont, Variables.ReplyColor, Variables.CodeFont, Variables.CodeColor, $"{messageline.Trim()}\n"); //display it using the reply font and color
                        }
                    }
                }
            }
        }

        /// <summary>
        /// DoSafe allows you to change the Safety levels of the Model.
        /// </summary>
        private static void DoSafe()
        {
            Size client = new(Variables.ClientSize.Width - 200, Variables.ClientSize.Height - 200); //get client size
            int xdiv = (client.Width - 40) / 3; //get horizontal division
            int ydiv = (client.Height - 70) / 6; //get vertical division
            safetyfrm.Location = new(50, 50); //set window location
            safetyfrm.ClientSize = client; //set window size
            safetyfrm.Text = "Model Safety Settings"; //set window title
            explainlb.Location = new(10, 10); //set explanation location
            explainlb.Size = new(xdiv, client.Width - 20); //set explanation size
            explainlb.Text = "Each of the numeric controls below corresponds to a type of Safety feature.  " +
                "Safety features work off a probability threshold.  For each such feature, you may select 0 to 3.  " +
                "0 is block nothing all the way to 3 for block low and above."; //set explanation text
            harasslbl.Location = new(10, ydiv + 20); //set harassment label location
            harasslbl.Size = new(xdiv, ydiv); //set harassment label size
            harasslbl.Text = "Harassment"; //set harassment label text
            sexuallbl.Location = new(10, 2 * ydiv + 30); //set sexually explicit label location
            sexuallbl.Size = new(xdiv, ydiv); //set sexually explicit label size
            sexuallbl.Text = "Sexually Explicit"; //set sexually explicit label text
            dangerlbl.Location = new(10, 3 * ydiv + 40); //set dangerous content label location
            dangerlbl.Size = new(xdiv, ydiv); //set dangerous content label size
            dangerlbl.Text = "Dangerous Content"; //set dangerous content label text
            hatesplbl.Location = new(10, 4 * ydiv + 50); //set hate speech label location
            hatesplbl.Size = new(xdiv, ydiv); //set hate speech label size
            hatesplbl.Text = "Hate Speech"; //set hate speech label text
            harassnud.Location = new(xdiv + 20, ydiv + 20); //set harassment numeric control location
            harassnud.Size = new(xdiv, ydiv); //set harassment numeric control size
            harassnud.Minimum = 0; //set harassment numeric control minimum
            harassnud.Maximum = 3; //set harassment numeric control maximum
            harassnud.Value = 0; //set harassment numeric control value
            harassnud.ValueChanged += (s, e) =>
            {
                harass = safetyconvert[(int)harassnud.Value]; //set new harass value
                explainha.Text = safetylevels[(int)harassnud.Value]; //set new text for explainha
            }; //set harassment numeric control function
            sexualnud.Location = new(xdiv + 20, 2 * ydiv + 30); //set sexually explicit numeric control location
            sexualnud.Size = new(xdiv, ydiv); //set sexually explicit numeric control size
            sexualnud.Minimum = 0; //set sexually explicit numeric control minimum
            sexualnud.Maximum = 3; //set sexually explicit numeric control maximum 
            sexualnud.Value = 0; //set sexually explicit numeric control value
            sexualnud.ValueChanged += (s, e) =>
            {
                sexual = safetyconvert[(int)sexualnud.Value]; //set new sexual value
                explainse.Text = safetylevels[(int)sexualnud.Value]; //set new text for explainse
            }; //set sexually explicit numeric control function
            dangernud.Location = new(xdiv + 20, 3 * ydiv + 40); //set dangerous content numeric control location
            dangernud.Size = new(xdiv, ydiv); //set dangerous content numeric control size
            dangernud.Minimum = 0; //set dangerous content numeric control minimum
            dangernud.Maximum = 3; //set dangerous content numeric control maximum
            dangernud.Value = 0; //set dangerous content numeric control value
            dangernud.ValueChanged += (s, e) =>
            {
                danger = safetyconvert[(int)dangernud.Value]; //set new danger value
                explainda.Text = safetylevels[(int)dangernud.Value]; //set new text for explainda
            }; //set dangerous content numeric control function
            hatespnud.Location = new(xdiv + 20, 4 * ydiv + 50); //set hate speech numeric control location
            hatespnud.Size = new(xdiv, ydiv); //set hate speech numeric control size
            hatespnud.Minimum = 0; //set hate speech numeric control minimum
            hatespnud.Maximum = 3; //set hate speech numeric control maximum
            hatespnud.Value = 0; //set hate speech numeric control value
            hatespnud.ValueChanged += (s, e) =>
            {
                hatesp = safetyconvert[(int)hatespnud.Value]; //set new harass value
                explainhs.Text = safetylevels[(int)hatespnud.Value]; //set new text for explainha
            }; //set hate speech numeric control function
            explainha.Location = new(2 * xdiv + 30, ydiv + 20); //set harassment explanatory label location
            explainha.Size = new(xdiv, ydiv); //set harassment explanatory label size
            explainha.Text = safetylevels[0]; //set harassment explanatory label text
            explainse.Location = new(2 * xdiv + 30, 2 * ydiv + 30); //set sexually explicit explanatory label location
            explainse.Size = new(xdiv, ydiv); //set sexually explicit explanatory label size
            explainse.Text = safetylevels[0]; //set sexually explicit explanatory label text
            explainda.Location = new(2 * xdiv + 30, 3 * ydiv + 40); //set dangerous content explanatory label location
            explainda.Size = new(xdiv, ydiv); //set dangerous content explanatory label size
            explainda.Text = safetylevels[0]; //set dangerous content explanatory label text
            explainhs.Location = new(2 * xdiv + 30, 4 * ydiv + 50); //set hate speech explanatory label location
            explainhs.Size = new(xdiv, ydiv); //set hate speech explanatory label size
            explainhs.Text = safetylevels[0]; //set hate speech explanatory label text
            readsafeties(); //read in current safeties from Model
            xdiv = (client.Width - 30) / 2; //get horizontal division
            acceptbtn.Location = new(10, 5 * ydiv + 60); //set accept button location
            acceptbtn.Size = new(xdiv, ydiv); //set accept button size
            acceptbtn.Text = "ACCEPT"; //set accept button text
            acceptbtn.Click += acceptbuttonclick; //set accept button function
            cancelbtn.Location = new(xdiv + 20, 5 * ydiv + 60); //set cancel button location
            cancelbtn.Size = new(xdiv, ydiv); //set cancel button size
            cancelbtn.Text = "CANCEL"; //set cancel button text
            cancelbtn.Click += cancelbuttonclick; //set cancel button function
            safetyfrm.Controls.Add(explainlb); //add explanation label to form
            safetyfrm.Controls.Add(harasslbl); //add harassment label to form
            safetyfrm.Controls.Add(harassnud); //add harassment numeric control to form
            safetyfrm.Controls.Add(explainha); //add harassment explanatory label to form
            safetyfrm.Controls.Add(sexuallbl); //add sexually explicit label to form
            safetyfrm.Controls.Add(sexualnud); //add sexually explicit numeric control to form
            safetyfrm.Controls.Add(explainse); //add sexually explicit explanatory label to form
            safetyfrm.Controls.Add(dangerlbl); //add dangerous content label to form
            safetyfrm.Controls.Add(dangernud); //add dangerous content numeric control to form
            safetyfrm.Controls.Add(explainda); //add dangerous content explanatory label to form
            safetyfrm.Controls.Add(hatesplbl); //add hate speech label to form
            safetyfrm.Controls.Add(hatespnud); //add hate speech numeric control to form
            safetyfrm.Controls.Add(explainhs); //add hate speech explanatory label to form
            safetyfrm.Controls.Add(acceptbtn); //add accept button to form
            safetyfrm.Controls.Add(cancelbtn); //add cancel button to form
            safetyfrm.AcceptButton = acceptbtn; //use accept button as form's accept button
            safetyfrm.CancelButton = cancelbtn; //use cancel button as form's cancel button
            safetyfrm.ShowDialog(); //show the form
            acceptbtn.Click -= acceptbuttonclick; //remove the accept button function
            Variables.Build(true); //build variables for model and chat session using chat history
            static void acceptbuttonclick(object? sender, EventArgs e) //local function for accept button click
            {
                Variables.Safety = [ //make a new set of Safety constraints
                    new() { Category = HarmCategory.HARM_CATEGORY_HARASSMENT, Threshold = harass }, //set harassment threshold
                    new() { Category = HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, Threshold = sexual }, //set sexually explicit threshold
                    new() { Category = HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, Threshold = danger }, //set dangerous content threshold
                    new() { Category = HarmCategory.HARM_CATEGORY_HATE_SPEECH, Threshold = hatesp } //set hate speech threshold
                ];
                Variables.Model.SafetySettings = Variables.Safety; //apply Safety constraints
                safetyfrm.Close(); //close the form
            }
            static void cancelbuttonclick(object? sender, EventArgs e) => safetyfrm.Close(); //local function for cancel button click closes form
            static void readsafeties() //local function to read the Model.SafetySettings and populate controls with its values
            {
                //Safety = Model.SafetySettings; // get current Safety settings
                string safetystr = $"There are {Variables.Safety.Length} Safety settings.  ";
                foreach (var setting in Variables.Safety)
                {
                    safetystr += $"\nCategory: {setting.Category}, Threshold: {setting.Threshold}";
                }
                MessageBox.Show(safetystr, "Safety Settings", MessageBoxButtons.OK);
                harass = Variables.Safety.First(c => c.Category == HarmCategory.HARM_CATEGORY_HARASSMENT).Threshold; // get harassment threshold
                sexual = Variables.Safety.First(c => c.Category == HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT).Threshold; //get sexually explicit threshold
                danger = Variables.Safety.First(c => c.Category == HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT).Threshold; //get dangerous content threshold
                hatesp = Variables.Safety.First(c => c.Category == HarmCategory.HARM_CATEGORY_HATE_SPEECH).Threshold; //get hate speech threshold
                harassnud.Value = safetyconvert.First(t => t.Value == harass).Key; //set harassnud.Value from harass
                sexualnud.Value = safetyconvert.First(t => t.Value == sexual).Key; //set sexualnud.Value from sexual
                dangernud.Value = safetyconvert.First(t => t.Value == danger).Key; //set dangernud.Value from danger
                hatespnud.Value = safetyconvert.First(t => t.Value == hatesp).Key; //set hatespnud.Value from hatesp
            }
        }

        /// <summary>
        /// DoParams allows you to change the Parameters of the Model.
        /// </summary>
        private static void DoParams()
        {
            Size client = new(Variables.ClientSize.Width - 200, Variables.ClientSize.Height - 200); //form client size
            int xdiv = (client.Width - 30) / 2; //get horizontal division
            int ydiv = (client.Height - 80) / 7; //get vertical division
            Form Paramform = new() //make new Parameter form
            {
                Location = new(50, 50), //set form location
                Size = client, //set form size
                Text = "Model Parameters" //set form title
            };
            Label numresplbl = new() //make number responses label
            {
                Location = new(10, 10), //set number responses label location
                Size = new(xdiv, ydiv), //set number responses label size
                Text = "Number Response Candidates" //set number responses label text
            };
            Paramform.Controls.Add(numresplbl); //add number responses label to form
            NumericUpDown numrespnud = new() //make number responses numeric control
            {
                Location = new(xdiv + 20, 10), //set number responses location
                Size = new(xdiv, ydiv), //set number responses size
                Minimum = 1, //set number responses minimum
                Maximum = 100, //set number responses maximum
                Value = 1 //set number responses value
            };
            Paramform.Controls.Add(numrespnud); //add number responses to form
            Label stopseqlbl = new() //make stop sequences label
            {
                Location = new(10, ydiv + 20), //set stop sequences label location
                Size = new(xdiv, ydiv), //set stop sequences label size
                Text = "Stop Sequences (up to 5)" //set stop sequences label text
            };
            Paramform.Controls.Add(stopseqlbl); //add stop sequences label to form
            List<string> combovals = ["<|endoftext|>", "\"\"\"\""]; //set up default values for stop sequences
            ComboBox stopseqcb = new() //make stop sequences combobox
            {
                Location = new(xdiv + 20, ydiv + 20), //set stop sequences location
                Size = new(xdiv, ydiv), //set stop sequences size
                MaxDropDownItems = 5 //set stop sequences maximum drop down items
            };
            stopseqcb.Items.AddRange(combovals.ToArray()); //set stop sequences items
            Paramform.Controls.Add(stopseqcb); //add stop sequences to form
            Label maxoutlbl = new() //make maximum output tokens label
            {
                Location = new(10, 2 * ydiv + 30), //set maximum output tokens label location
                Size = new(xdiv, ydiv), //set maximum output tokens label size
                Text = "Maximum Output Tokens" //set maximum output tokens label text
            };
            Paramform.Controls.Add(maxoutlbl); //add maximum output tokens label to form
            NumericUpDown maxoutnud = new() //make maximum output tokens numeric control
            {
                Location = new(xdiv + 20, 2 * ydiv + 30), //set maximum output tokens location
                Size = new(xdiv, ydiv), //set maximum output tokens size
                Minimum = 0, //set maximum output tokens minimum
                Maximum = 4096, //set maximum output tokens maximum
                Value = 256, //set maximum output tokens value
                Increment = 16 //set maximum output tokens increment
            };
            Paramform.Controls.Add(maxoutnud); //add maximum output tokens to form
            Label templbl = new() //make temperature label
            {
                Location = new(10, 3 * ydiv + 40), //set temperature label location
                Size = new(xdiv, ydiv), //set temperature label size
                Text = "Temperature" //set temperature label text
            };
            Paramform.Controls.Add(templbl); //add termperature label to form
            NumericUpDown tempnud = new() //make temperature numeric control
            {
                Location = new(xdiv + 20, 3 * ydiv + 40), //set temperature location
                Size = new(xdiv, ydiv), //set temperature size
                Minimum = 0, //set temperature minimum
                Maximum = 1, //set temperature maximum
                Value = 0.05m, //set temperature value
                DecimalPlaces = 2, //set temperature decimal places
                Increment = 0.01m //set temperature increment
            };
            Paramform.Controls.Add(tempnud); //add temperature to form
            Label topplbl = new() //make top p label
            {
                Location = new(10, 4 * ydiv + 50), //set top p label location
                Size = new(xdiv, ydiv), //set top p label size
                Text = "Top P" //set top p label text
            };
            Paramform.Controls.Add(topplbl); //add top p label to form
            NumericUpDown toppnud = new() //make top p numeric control
            {
                Location = new(xdiv + 20, 4 * ydiv + 50), //set top p location
                Size = new(xdiv, ydiv), //set top p size
                Minimum = 0, //set top p minimum
                Maximum = 1, //set top p maximum
                Value = 0.9m, //set top p value
                DecimalPlaces = 2, //set top p decimal places
                Increment = 0.01m //set top p increment
            };
            Paramform.Controls.Add(toppnud); //add top p to form
            Label topklbl = new() //make top k label
            {
                Location = new(10, 5 * ydiv + 60), //set top k label location
                Size = new(xdiv, ydiv), //set top k label size
                Text = "Top K" //set top k label text
            };
            Paramform.Controls.Add(topklbl); //add top k label to form
            NumericUpDown topknud = new() //make top k numeric control
            {
                Location = new(xdiv + 20, 5 * ydiv + 60), //set top k location
                Size = new(xdiv, ydiv), //set top k size
                Minimum = 0, //set top k minimum
                Maximum = 100, //set top k maximum
                Value = 1 //set top k value
            };
            Paramform.Controls.Add(topknud); //add top k to form
            if (Variables.Model.Config.CandidateCount != null) //if CandidateCount exists
            {
                numrespnud.Value = (decimal)Variables.Model.Config.CandidateCount; //set number responses from CandidateCount
            }
            if (Variables.Model.Config.StopSequences != null) //if StopSequences exists
            {
                stopseqcb.BeginUpdate(); //begin updating stop sequences
                stopseqcb.Items.Clear(); //clear stop sequences
                stopseqcb.Items.AddRange(Variables.Model.Config.StopSequences); //add StopSequences to stop sequences
                stopseqcb.EndUpdate(); //end updating stop sequences
            }
            if (Variables.Model.Config.MaxOutputTokens != null) //if MaxOutputTokens exists
            {
                maxoutnud.Value = (decimal)Variables.Model.Config.MaxOutputTokens; //set maximum output tokens from MaxOutputTokens
            }
            if (Variables.Model.Config.Temperature != null) //if Temperature exists
            {
                tempnud.Value = (decimal)Variables.Model.Config.Temperature; //set temperature from Temperature
            }
            if (Variables.Model.Config.TopP != null) //if TopP exists
            {
                toppnud.Value = (decimal)Variables.Model.Config.TopP; //set top p from TopP
            }
            if (Variables.Model.Config.TopK != null) //if TopK exists
            {
                topknud.Value = (decimal)Variables.Model.Config.TopK; //set top k from TopK
            }
            Button acceptbtn = new() //make accept button
            {
                Location = new(10, 6 * ydiv + 70), //set accept button location
                Size = new(xdiv, ydiv), //set accept button size
                Text = "ACCEPT" //set accept button text
            };
            acceptbtn.Click += (s, e) => //set event handler for accept button
            {
                Variables.Model.Config.CandidateCount = (int)numrespnud.Value; //set CandidateCount
                Variables.Model.Config.StopSequences = stopseqcb.Items.OfType<object>().Where(item => item != null).Select(item => item.ToString()!).ToArray(); //set StopSequences
                Variables.Model.Config.MaxOutputTokens = (int)maxoutnud.Value; //set MaxOutputTokens
                Variables.Model.Config.Temperature = (double)tempnud.Value; //set Temperature
                Variables.Model.Config.TopP = (double)toppnud.Value; //set TopP
                Variables.Model.Config.TopK = (double)(int)topknud.Value; //set TopK
                int workaroundhere;
                if (Variables.Model.Config.TopK == 0) //workaround for negative top k error
                {
                    Variables.Model.Config.TopK = 1; //set top k to 1
                }
                Variables.ModelConfig = Variables.Model.Config; //make a copy of the model's configuration
                Paramform.Close(); //close the form
            };
            Paramform.Controls.Add(acceptbtn); //add accept button to form
            Button cancelbtn = new() //make cancel button
            {
                Location = new(xdiv + 20, 6 * ydiv + 70), //set cancel button location
                Size = new(xdiv, ydiv), //set cancel button size
                Text = "CANCEL" //set cancel button text
            };
            cancelbtn.Click += (s, e) => Paramform.Close(); //set event handler for cancel button
            Paramform.Controls.Add(cancelbtn); //add cancel button to form
            Paramform.AcceptButton = acceptbtn; //use accept button for form validation
            Paramform.CancelButton = cancelbtn; //use cancel button for form cancelation
            Paramform.ShowDialog(); //show the form
            Variables.Build(true); //build variables for model and chat using chat history
        }

        /// <summary>
        /// DoSettings allows you to change the program settings.
        /// </summary>
        private static void DoSettings()
        {
            Font userf = Variables.UserFont, replyf = Variables.ReplyFont, helpf = Variables.HelpFont, debugf = Variables.DebugFont, codef = Variables.CodeFont; //fonts
            Color backc = sresults.BackColor, userc = Variables.UserColor, replyc = Variables.ReplyColor, helpc = Variables.HelpColor, helpc2 = Variables.HelpColor2, debugc = Variables.DebugColor, codec = Variables.CodeColor; //var for color
            Size client = new(Variables.ClientSize.Width - 200, Variables.ClientSize.Height - 200); //get client size
            int xdiv = (client.Width - 40) / 3; //set up horizontal division
            int ydiv = (client.Height - 90) / 8; //set up vertical division
            Form settingform = new() //make a form for the settings
            {
                Location = new(50, 50), //set the location
                ClientSize = client, //set the client size
                Text = "Settings" //set the title
            };
            CheckBox clearfirstcb = new() //make a checkbox for ClearFirst
            {
                Location = new(10, 10), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Clear First", //set the text
                Checked = Variables.ClearFirst //set the checked state
            };
            settingform.Controls.Add(clearfirstcb); //add the clear first checkbox to the form
            settingform.Controls.Add(new Label() //make a new label and add it to the form
            {
                Location = new(xdiv + 20, 10), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "API key:" //set the text
            });
            TextBox apikeytb = new() //make a textbox for APIkey
            {
                Location = new(2 * xdiv + 30, 10), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = Variables.APIkey, //set the text to APIkey
                PlaceholderText = "Enter API key here", //some placeholder text to help the user
            };
            settingform.Controls.Add(apikeytb); //add the api key textbox to the form
            Button userfontb = new() //make a button for user font
            {
                Location = new(10, ydiv + 20), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "User Font" //set the text
            };
            userfontb.Click += (s, e) => userf = getfont(Variables.UserFont); //set a lambda function for the user font button
            settingform.Controls.Add(userfontb); //add the user font button to the form
            Button usercolorb = new() //make a button for user color
            {
                Location = new(2 * xdiv + 30, ydiv + 20), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "User Color" //set the text
            };
            usercolorb.Click += (s, e) => userc = getcolor(Variables.UserColor); //set a lambda function for the user color button
            settingform.Controls.Add(usercolorb); //add the user color button to the form
            Button replyfontb = new() //make a button for reply font
            {
                Location = new(10, 2 * ydiv + 30), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Reply Font" //set the text
            };
            replyfontb.Click += (s, e) => replyf = getfont(Variables.ReplyFont); //set a lambda function for the reply font button
            settingform.Controls.Add(replyfontb); //add the reply font button to the form
            Button replycolorb = new() //make a button for the reply color
            {
                Location = new(2 * xdiv + 30, 2 * ydiv + 30), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Reply Color" //set the text
            };
            replycolorb.Click += (s, e) => replyc = getcolor(Variables.ReplyColor); //set a lambda function for the reply color button
            settingform.Controls.Add(replycolorb); //add the reply color button to the form
            Button helpfontb = new() //make a button for the help font
            {
                Location = new(10, 3 * ydiv + 40), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Help Font" //set the text
            };
            helpfontb.Click += (s, e) => helpf = getfont(Variables.HelpFont); //set a lambda function for the help font button
            settingform.Controls.Add(helpfontb); //add the help font button to the form
            Button helpcolorb = new() //make a button for the help color
            {
                Location = new(xdiv + 20, 3 * ydiv + 40), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Help Color" //set the text
            };
            helpcolorb.Click += (s, e) => helpc = getcolor(Variables.HelpColor); //set a lambda function for the help color button
            settingform.Controls.Add(helpcolorb); //add the help color button to the form
            Button helpcolor2b = new() //make a button for the second help color
            {
                Location = new(2 * xdiv + 30, 3 * ydiv + 40), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Help Color 2" //set the text
            };
            helpcolor2b.Click += (s, e) => helpc2 = getcolor(Variables.HelpColor2); //set a lambda function for the second help color button
            settingform.Controls.Add(helpcolor2b); //add the second help color button to the form
            Button debugfontb = new() //make a button for the debug font
            {
                Location = new(10, 4 * ydiv + 50), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Debug Font" //set the text
            };
            debugfontb.Click += (s, e) => debugf = getfont(Variables.DebugFont); //set a lambda function for the debug font button
            settingform.Controls.Add(debugfontb); //add the debug font button to the form
            Button debugcolorb = new() //make a button for the debug color
            {
                Location = new(2 * xdiv + 30, 4 * ydiv + 50), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Debug Color" //set the text
            };
            debugcolorb.Click += (s, e) => debugc = getcolor(Variables.DebugColor); //set a lambda function for the debug color button
            settingform.Controls.Add(debugcolorb); //add the debug color button to the form
            Button codefontb = new() //make a button for the code font
            {
                Location = new(10, 5 * ydiv + 60), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Code Font" //set the text
            };
            codefontb.Click += (s, e) => codef = getfont(Variables.CodeFont); //set a lambda function for the code font button
            settingform.Controls.Add(codefontb); //add the code font button to the form
            Button codecolorb = new() //make a button for the code color
            {
                Location = new(2 * xdiv + 30, 5 * ydiv + 60), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Code Color" //set the text
            };
            codecolorb.Click += (s, e) => codec = getcolor(Variables.CodeColor); //set a lambda function for the code color button
            settingform.Controls.Add(codecolorb); //add the code color button to the form
            Button backcolorb = new() //make a button for the background color
            {
                Location = new(xdiv + 20, 6 * ydiv + 70), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "Background Color" //set the text
            };
            backcolorb.Click += (s, e) => backc = getcolor(sresults.BackColor); //set a lambda function for the background color button
            settingform.Controls.Add(backcolorb); //add the background color button to the form
            Button acceptb = new() //make a button for accepting
            {
                Location = new(10, 7 * ydiv + 80), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "ACCEPT" //set the text
            };
            acceptb.Click += (s, e) => //set a lambda function for the accept button
            {
                Variables.ClearFirst = clearfirstcb.Checked; //set the ClearFirst flag
                Variables.APIkey = apikeytb.Text; //set the APIkey
                Variables.BackColor = backc; //set the background color
                Variables.UserColor = userc; //set the UserColor
                Variables.UserFont = userf; //set the UserFont
                Variables.ReplyColor = replyc; //set the ReplyColor
                Variables.ReplyFont = replyf; //set the ReplyFont
                Variables.HelpColor = helpc; //set the HelpColor
                Variables.HelpFont = helpf; //set the HelpFont
                Variables.HelpColor2 = helpc2; //set the HelpColor2
                Variables.DebugColor = debugc; //set the DebugColor
                Variables.DebugFont = debugf; //set the DebugFont
                Variables.CodeColor = codec; //set the CodeColor
                Variables.CodeFont = codef; //set the CodeFont
                settingform.Close(); //close the form
                DoShow(); //show the conversation
            };
            settingform.Controls.Add(acceptb); //add the accept button to the form
            Button cancelb = new() //make a button for cancelling
            {
                Location = new(2 * xdiv + 30, 7 * ydiv + 80), //set the location
                Size = new(xdiv, ydiv), //set the size
                Text = "CANCEL" //set the text
            };
            cancelb.Click += (s, e) => settingform.Close(); //make a lambda function for the cancel button
            settingform.Controls.Add(cancelb); //add the cancel button to the form
            settingform.ShowDialog(); //show the form
            static Font getfont(Font oldfont) //local function to return a font taking the old font as a Parameter
            {
                using FontDialog fontdialog = new(); //use a font dialog
                {
                    fontdialog.Font = oldfont; //set the old font as the initial font
                    if (fontdialog.ShowDialog() == DialogResult.OK) //if the user clicked OK
                    {
                        return fontdialog.Font; //return the font selected
                    }
                }
                return oldfont; //return the old font
            }
            static Color getcolor(Color oldcolor) //local function to return a color taking the old color as a Parameter
            {
                using ColorDialog colordialog = new(); //use a color dialog
                {
                    colordialog.Color = oldcolor; //set the old color as the initial color
                    if (colordialog.ShowDialog() == DialogResult.OK) //if the user clicked OK
                    {
                        return colordialog.Color; //return the color selected
                    }
                }
                return oldcolor; //return the old color
            }
        }

        [GeneratedRegex(@"^/(\w+)(?:\s(.+\n?))?$")] // old version: [GeneratedRegex(@"^/(\w+)\s(.+\n?)$")]
        private static partial Regex CommandRegex(); //a generated regular expression for parsing Commands
    }

    public class Variables
    {
        public static bool SizeReady { get => _sizeready; set => _sizeready = value; }
        public static bool ClearFirst { get => _clearfirst; set => _clearfirst = value; }
        public static string? APIkey { get => _apikey; set => _apikey = value; }
        public static GenerationConfig ModelConfig { get => _modelconfig; set => _modelconfig = value; }
        public static SafetySetting[] Safety { get => _safety; set => _safety = value; }
        [JsonIgnore]
        public static GeminiProModel Model { get => _model; set => _model = value; }
        [JsonIgnore]
        public static ChatSession Chat { get => _chat; set => _chat = value; }
        public static Font UserFont { get => _userfont; set => _userfont = value; }
        public static Font ReplyFont { get => _replyfont; set => _replyfont = value; }
        public static Font HelpFont { get => _helpfont; set => _helpfont = value; }
        public static Font DebugFont { get => _debugfont; set => _debugfont = value; }
        public static Font CodeFont { get => _codefont; set => _codefont = value; }
        public static Color BackColor { get => _backcolor; set => _backcolor = value; }
        public static Color UserColor { get => _usercolor; set => _usercolor = value; }
        public static Color ReplyColor { get => _replycolor; set => _replycolor = value; }
        public static Color HelpColor { get => _helpcolor; set => _helpcolor = value; }
        public static Color HelpColor2 { get => _helpcolor2; set => _helpcolor2 = value; }
        public static Color DebugColor { get => _debugcolor; set => _debugcolor = value; }
        public static Color CodeColor { get => _codecolor; set => _codecolor = value; }
        public static string Query { get => _query; set => _query = value; }
        public static string Command { get => _command; set => _command = value; }
        public static string Param { get => _param; set => _param = value; }
        public static Size ClientSize { get => _clientsize; set => _clientsize = value; }
        [JsonIgnore]
        public static List<Content> History { get => _history; set => _history = value; }

        private static bool _sizeready; //this bool prevents a fatal loop during initialization
        private static bool _clearfirst; //this bool is used for whether the results area should be cleared before output
        private static string? _apikey; //will force calling DoAPIKey "AIzaSyBAEyxDoJBb2axp-xEHxnu0sRZF-dQs-uQ" or some such
        private static GenerationConfig _modelconfig;
        private static SafetySetting[] _safety; //a set of safety settings
        [JsonIgnore]
        private static GeminiProModel _model; //load the model
        [JsonIgnore]
        private static ChatSession _chat; //the chat session
        private static Font _userfont, _replyfont, _helpfont, _debugfont, _codefont; //fonts
        private static Color _backcolor, _usercolor, _replycolor, _helpcolor, _helpcolor2, _debugcolor, _codecolor; //colors
        private static string _query, _command, _param; //this is the variable used to hold the query, the command, and param(s) gotten via ParseCommand method
        private static Size _clientsize; //static storage for ClientSize
        [JsonIgnore]
        private static List<Content> _history; //the chat history (do we need this since we have chat?)

        static Variables()
        {
            _sizeready = false; //SizeReady is false to begin with - changed in GC_Load
            _clearfirst = false; //ClearFirst is false
            _apikey = null; //apikey is null which will force DoAPIKey to be called
            _modelconfig = new(); //This holds values for model parameters like temperature, top p, etc.
            _safety =
              [ new() { Category = HarmCategory.HARM_CATEGORY_HARASSMENT, Threshold = HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE },
                new() { Category = HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT, Threshold = HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE },
                new() { Category = HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT, Threshold = HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE },
                new() { Category = HarmCategory.HARM_CATEGORY_HATE_SPEECH, Threshold = HarmBlockThreshold.BLOCK_MEDIUM_AND_ABOVE }]; //a default safety threshold set
            _model = GeminiChat.GeminiCreate(APIkey); //Model is created here
            Model.SafetySettings = Safety; //put the safety settings in the model
            Model.Config = ModelConfig; //put the model config in the model
            _chat = Model.StartChat(new StartChatParams()); //Chat is created here
            _userfont = new("Arial Black", 12); //UserFont is used for user queries
            _replyfont = new("Arial", 13); //ReplyFont is used for model replies
            _helpfont = new("Arial Black", 13); //HelpFont is used for help text obtained with /help
            _debugfont = new("Arial Black", 15); //DebugFont is used to display debug or error information
            _codefont = new("Cascadia Mono", 13); //CodeFont is used for code
            _backcolor = Color.Black; //BackColor is used for background color of results control
            _usercolor = Color.Aqua; //UserColor is used for user queries
            _replycolor = Color.White; //ReplyColor is used for model replies
            _helpcolor = Color.Lime; //HelpColor is used for help text obtained with /help
            _helpcolor2 = Color.Yellow; //HelpColor2 is used for help text obtained with /help
            _debugcolor = Color.LightSalmon; //DebugColor is used for debug or error messages
            _codecolor = Color.Orange; //CodeColor is used for code
            _query = _command = _param = string.Empty; //Query, Command, and Param are used for user queries, commands, and parameter(s)
            _history = Chat.History; //History is the chat history
        }

        /// <summary>
        /// default constructor - not really needed, but figured I'd put it in because I cannot make the class static.
        /// </summary>
        public Variables() { }

        /// <summary>
        /// Save serializes the variables and writes them to the specified file.
        /// </summary>
        /// <param name="filename"></param> This is the specified file name.
        public void Save(string filename)
        {
            Save1(filename); //first, let's save the variables
            string json = JsonSerializer.Serialize(Chat.History); //serialize the chat history to a string
            File.WriteAllText(filename, json); //write the file with the string
        }

        public void Save1(string filename)
        {
            string fname = Path.ChangeExtension(filename, "chatsettings"); //change the extension to chatsettings
            string json = JsonSerializer.Serialize(this); //serialize the variables to a string
            File.WriteAllText(fname, json); //write the file with the string
        }

        /// <summary>
        /// Load reads the specified file and uses the results to deserialize to the variables, creating a new instance and passing the value of that instance.
        /// </summary>
        /// <param name="filename"></param> This is the specified file name.
        /// <returns>Variables?</returns> A nullable instance of the variables.  Null if there was an error during deserialization.
        public static Variables? Load(string filename)
        {
            if (File.Exists(filename))
            {
                Variables? newvar = Load1(filename); //get the variables from the file
                string json = File.ReadAllText(filename); //read the specified file to a string
                List<Content>? history = JsonSerializer.Deserialize<List<Content>>(json); //deserialize the variables from the string and create a new instance
                if (history == null) //if there was an error during deserialization
                {
                    MessageBox.Show("Couldn't load chat history.", "Error", MessageBoxButtons.OK); //show the error message
                    return null; //return null
                }
                History = history; //set history variable
                Build(false); //build the variables model and chat session from the other variables
                Chat.History.Clear(); //clear the chat history
                Chat.History.AddRange(history); //add the history to the chat history
                return newvar; //return the new instance
            }
            return null; //return null
        }

        public static Variables? Load1(string filename)
        {
            string fname = Path.ChangeExtension(filename, "chatsettings"); //change the extension to chatsettings
            string json = File.ReadAllText(fname); //read the specified file to a string
            return JsonSerializer.Deserialize<Variables>(json); //deserialize the variables from the string and create a new instance, which we will return
        }

        public static void Build(bool gethistory)
        {
            if (gethistory) //if getting history
            {
                History = Chat.History; //first, we need to get the chat history
            }
            APIkey ??= ""; //set API key to an empty string if it is null
            Model = new(APIkey) { Config = ModelConfig, SafetySettings = Safety }; //create a new model using the API key, model configuration parameters, and safety settings
            Chat = Model.StartChat(new StartChatParams()); //use the model to create a new chat session
            Chat.History.Clear(); //clear the chat history
            Chat.History.AddRange(History); //add the history to chat history
        }
    }
}
