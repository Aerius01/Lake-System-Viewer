using System;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;
using Npgsql;
using System.Data;
using TMPro;
using Unity;

public class DBHandler : MonoBehaviour
{
    private bool connected = false, verified = false; 
    private TMP_InputField hostInput, usernameInput, passwordInput, DBNameInput;
    private GameObject testBuffer, verifyBuffer;

    [SerializeField] private Main main;
    [SerializeField] private Button connectButton, verifyButton, startButton;
    [SerializeField] private GameObject menuPanel, messageBox, loadingBar, verifyBox;

    private void Awake()
    {
        this.hostInput = menuPanel.transform.Find("Host").transform.Find("Input").GetComponent<TMP_InputField>();
        this.usernameInput = menuPanel.transform.Find("Username").transform.Find("Input").GetComponent<TMP_InputField>();
        this.passwordInput = menuPanel.transform.Find("Password").transform.Find("Input").GetComponent<TMP_InputField>();
        this.DBNameInput = menuPanel.transform.Find("DatabaseName").transform.Find("Input").GetComponent<TMP_InputField>();

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

        string connString = string.Format("Host={0};Username={1};Password={2};Database={3};", hostInput.text, usernameInput.text, passwordInput.text, DBNameInput.text);
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

        messageBox.transform.Find("Title").GetComponent<TextMeshProUGUI>().text = stateTitle;
        messageBox.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = stateDescription;

        this.testBuffer.SetActive(false);
        messageBox.SetActive(true);

        if (this.connected) this.verifyButton.interactable = true;
    }

    public async void VerifyButton()
    {
        this.menuPanel.GetComponent<CanvasGroup>().interactable = false;

        string connString = string.Format("Host={0};Username={1};Password={2};Database={3};CommandTimeout=0;Pooling=true;MaxPoolSize=5000;Timeout=300", hostInput.text, usernameInput.text, passwordInput.text, DBNameInput.text);
        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            // Run through checklist of tables
            TableImports tableImports = new TableImports(connection);
            await tableImports.VerifyTables();

            await connection.CloseAsync();
        }

        this.menuPanel.GetComponent<CanvasGroup>().interactable = true;
    }


    // verify table status --> verify structure
    // verify nullity/data quality of tables
    // start --> saves table bools, saves connectivity data, activates MAIN


    // change any input field --> restart
    // pop up box disables everything until "okay" clicked

    private void CheckButtonStatuses()
    {
        // Button disables
        if (!connected && (verifyButton.interactable || startButton.interactable))
        {
            verifyButton.interactable = false;
            startButton.interactable = false;
        }
        else if (!verified && startButton.interactable)
        {
            startButton.interactable = false;
        }
    }

}