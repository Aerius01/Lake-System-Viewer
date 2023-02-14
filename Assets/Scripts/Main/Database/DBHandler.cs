using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using Npgsql;
using System.Data;
using TMPro;
using Unity;
using System.Threading.Tasks;

public class DBHandler : MonoBehaviour
{
    private bool connected = false, verified = false, successfulInit = false; 
    private TMP_InputField hostInput, usernameInput, passwordInput, DBNameInput;
    private GameObject testBuffer, verifyBuffer;
    private TableImports tableImports;
    private Color green = new Color(65f/255f, 170f/255f, 55f/255f, 1f), yellow = new Color(1f, 220f/255f, 0f, 1f), red = new Color(220f/255f, 0f, 0f, 1f);

    [SerializeField] private Main main;
    [SerializeField] private Button connectButton, verifyButton, startButton;
    [SerializeField] private GameObject menuPanel, messageBox, verifyBox, textPrefab, statusContainer, background, startBufferer;
    [SerializeField] private LoaderBar loadingBar;

    private void Awake()
    {
        this.hostInput = menuPanel.transform.Find("Information").transform.Find("Host").transform.Find("Input").GetComponent<TMP_InputField>();
        this.usernameInput = menuPanel.transform.Find("Information").transform.Find("Username").transform.Find("Input").GetComponent<TMP_InputField>();
        this.passwordInput = menuPanel.transform.Find("Information").transform.Find("Password").transform.Find("Input").GetComponent<TMP_InputField>();
        this.DBNameInput = menuPanel.transform.Find("Information").transform.Find("DatabaseName").transform.Find("Input").GetComponent<TMP_InputField>();

        this.testBuffer = connectButton.transform.Find("BufferIcon").gameObject;
        this.verifyBuffer = verifyButton.transform.Find("BufferIcon").gameObject;

        this.testBuffer.SetActive(false);
        this.verifyBuffer.SetActive(false);
    }

    private void Update()
    {
        // Control button statuses
        this.CheckButtonStatuses();   
    }

    // Called when any input field is updated
    public void NewInput()
    {
        this.connected = false;
        this.verified = false;
    }

    public async void TestButton()
    {
        this.menuPanel.GetComponent<CanvasGroup>().interactable = false;
        this.testBuffer.SetActive(true);

        string connString = string.Format("Host={0};Username={1};Password={2};Database={3};Pooling=false;", hostInput.text, usernameInput.text, passwordInput.text, DBNameInput.text);
        string stateTitle = "";
        string stateDescription = "";

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (connection.State != ConnectionState.Open)
            {
                // This connection tends to have problems. Attempt to re-open the connection if it fails
                int runningCount = 0;
                while (runningCount < 30)
                {
                    runningCount++;
                    try 
                    {
                        await connection.OpenAsync();
                        this.connected = true;
                        stateTitle = "Success!";
                        stateDescription = "The connection was successful. You can now verify the database structure.";
                        break;
                    }
                    catch (SocketException e) { Debug.Log(e.Message); break; }// IP issue}
                    catch (PostgresException e)
                    { 
                        if (e.SqlState == "28000") 
                        { 
                            stateTitle = "Failed to connect!";
                            stateDescription = "Resolved the host, but either the username or the database name provided are incorrect."; 
                            break; 
                        }
                        else if (e.SqlState == "28P01")
                        {
                            stateTitle = "Failed to connect!";
                            stateDescription = "Provided password is incorrect."; 
                            break;
                        }
                    }
                    catch (Npgsql.NpgsqlException)
                    {
                        stateTitle = "Failed to connect!";
                        stateDescription = "The host could not be resolved - there was no answer at the provided IP. Is the IP address typed in correctly?"; 
                        break;
                    }
                }
            }

            await connection.CloseAsync();
        }

        messageBox.transform.Find("Image").transform.Find("Title").GetComponent<TextMeshProUGUI>().text = stateTitle;
        messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = stateDescription;

        this.testBuffer.SetActive(false);
        messageBox.SetActive(true);
        this.menuPanel.GetComponent<CanvasGroup>().alpha = 0.3f;

        if (this.connected) this.verifyButton.interactable = true;
    }

    public async void VerifyButton()
    {
        this.menuPanel.GetComponent<CanvasGroup>().interactable = false;
        this.verifyBuffer.SetActive(true);

        string connString = string.Format("Host={0};Username={1};Password={2};Database={3};CommandTimeout=0;Pooling=false;Timeout=300", hostInput.text, usernameInput.text, passwordInput.text, DBNameInput.text);
        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            // Run through checklist of tables
            try
            {
                tableImports = new TableImports(connection, loadingBar);
                bool requiredTables = await tableImports.VerifyTables();

                if (TableImports.tables.Keys.Count == 0) throw new NpgsqlException();

                // Delete any existing contents (ie, run already executed), and scroll window to top
                foreach (Transform child in this.statusContainer.transform) Destroy(child.gameObject); 
                Vector3 currentPos = this.statusContainer.transform.parent.localPosition;
                currentPos.y = 0f;
                this.statusContainer.transform.parent.localPosition = currentPos;

                // Create new status indicators
                foreach (string key in TableImports.tables.Keys)
                {
                    GameObject statusIndicator = (Instantiate (textPrefab) as GameObject);
                    statusIndicator.transform.SetParent(this.statusContainer.transform, false);

                    // Grab the relevant prefab sections
                    Image light = statusIndicator.transform.Find("Light").GetComponent<Image>();
                    TextMeshProUGUI name = statusIndicator.transform.Find("Column").transform.Find("TableName").GetComponent<TextMeshProUGUI>();
                    TextMeshProUGUI message = statusIndicator.transform.Find("Column").transform.Find("TableMessage").GetComponent<TextMeshProUGUI>();

                    // Update those sections
                    Table table = TableImports.tables[key];

                    if (table.lightColor == 0) light.color = green;
                    else if (table.lightColor == 1) light.color = yellow;
                    else light.color = red;

                    name.text = table.tableName;
                    message.text = table.tableMessage;
                } 

                this.verified = requiredTables;
                this.verifyBox.SetActive(true);
            }
            catch (Npgsql.NpgsqlException e)
            {
                if (e.InnerException is System.IO.IOException) // Connection interrupted while reading
                {
                    messageBox.transform.Find("Image").transform.Find("Title").GetComponent<TextMeshProUGUI>().text = "Connection Interrupted";
                    messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = "The connection was interrupted while attempting to verify the tables. Please re-test the connection.";
                }
                else
                {
                    messageBox.transform.Find("Image").transform.Find("Title").GetComponent<TextMeshProUGUI>().text = "Connection Issue";
                    messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = "The database could not be contacted at the verify step. It's unclear why, given that the database was contactable during the TEST step. Please re-test and try again.";
                }

                messageBox.SetActive(true);
                this.NewInput();
            }
            catch (Exception)
            {
                messageBox.transform.Find("Image").transform.Find("Title").GetComponent<TextMeshProUGUI>().text = "Unhandled Exception";
                messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = "Something unexpected and unaccounted for has occurred. Please restart the process. If the error persists, maybe try restarting the application or your computer.";
                
                messageBox.SetActive(true);
                this.NewInput();
            }

            this.menuPanel.GetComponent<CanvasGroup>().alpha = 0.3f;
            await connection.CloseAsync();
        }

        this.verifyBuffer.SetActive(false);
        if (this.verified) this.startButton.interactable = true;
    }

    public async void StartButton()
    {
        this.menuPanel.GetComponent<CanvasGroup>().interactable = false;
        this.menuPanel.GetComponent<CanvasGroup>().alpha = 0.3f;

        string connString = string.Format("Host={0};Username={1};Password={2};Database={3};Pooling=true;MaxPoolSize=5000;CommandTimeout=0", hostInput.text, usernameInput.text, passwordInput.text, DBNameInput.text);
        DatabaseConnection.SetConnectionString(connString);

        this.startBufferer.SetActive(true);
        try { this.successfulInit = await this.main.Initialize(); }
        catch (Exception)
        { 
            this.successfulInit = false; 
            this.startBufferer.SetActive(false);
        }

        this.startBufferer.SetActive(false);

        if (!this.successfulInit)
        {
            this.main.ClearAll();
            
            messageBox.transform.Find("Image").transform.Find("Title").GetComponent<TextMeshProUGUI>().text = "Import Error";
            messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = string.Format("There was an issue while importing either the \"{0}\" or the \"{1}\" tables. It is likely that the connection was interrupted or the connected database has a limited number of pooled connections available. Please restart the verification process.", TableImports.checkTables[0], TableImports.checkTables[1]);
            
            messageBox.SetActive(true);
            this.NewInput();
        }
        else 
        {
            List<string> failedTables = new List<string>();
            foreach (Table table in TableImports.tables.Values) { if (!table.imported) failedTables.Add(table.tableName); }

            string message = "All initializations went as expected!";
            if (failedTables.Count != 0) 
            { 
                if (failedTables.Count > 1) { message = string.Format("Not all tables imported/initialized correctly. The following tables (and the functionality tied to them) will therefore be disabled: \"{0}\". These are optional tables and so the render will still proceed.", string.Join("\", \"", failedTables)); }
                else { message = string.Format("Not all tables imported/initialized correctly. The following table (and the functionality tied to it) will therefore be disabled: \"{0}\". This is an optional table and so the render will still proceed.", failedTables[0]); }
            }

            messageBox.transform.Find("Image").transform.Find("Title").GetComponent<TextMeshProUGUI>().text = "Initializations";
            messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = message;

            messageBox.SetActive(true);
        }
    }

    public void MessageBoxButton ()
    {
        if (this.successfulInit)
        {
            TimeManager.instance.PlayButton();
            this.main.GoodToGo(); // allow updates to start
            this.gameObject.SetActive(false); // disable import canvas
        }
    }

    private void CheckButtonStatuses()
    {
        // Button disables
        if (!this.connected && (this.verifyButton.interactable || this.startButton.interactable))
        {
            this.verifyButton.interactable = false;
            this.startButton.interactable = false;
        }
        else if (!this.verified && this.startButton.interactable)
        {
            this.startButton.interactable = false;
        }
    }

}