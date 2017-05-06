using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceModel.Web;
using System.ServiceModel;
using Nancy.Hosting.Wcf;
using System.Timers;
using System.Threading;
using System.Globalization;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace SmartLock
{
    public partial class Form1 : Form
    {
        SmartLockDatabaseDataSet myDataSet;
        SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter myAdmin;
        SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter myLogs;
        SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter myLocks;
        Thread logThread;
        Thread checkLocksThread;
        List<int[]> errorLocks = new List<int[]>();
        int lastSeenLog = 0;

        Telegram.Bot.TelegramBotClient Bot;
        int BotEnabled = 0;
        int BotErrorSegnalated = 0;


        public Form1()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form1_Closing);
            //this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            


        }

        private void Form1_Shown(object sender, System.EventArgs e)
        //private void Form1_Load(object sender, System.EventArgs e)
        //protected override void OnLoad(EventArgs e)
        {
            this.Refresh();
            Console.WriteLine("Hello!");
            textBox1.AppendText("Form loaded...");
            textBox1.AppendText(Environment.NewLine);

            textBox1.AppendText("Connecting to the database...");
            textBox1.AppendText(Environment.NewLine);

            myDataSet = new SmartLockDatabaseDataSet();
            myAdmin = new SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter();
            myLogs = new SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter();
            myLocks = new SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter();

            if (!TryDatabaseConnection())
            {
                SmartLock.Properties.Settings.Default["SmartLockDatabaseConnectionString"] = "Data Source=(localdb)\\v13.0;AttachDbFilename=|DataDirectory|\\SmartLockDatabase.mdf;Integrated Security=True";
                myDataSet = new SmartLockDatabaseDataSet();
                myAdmin = new SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter();
                myLogs = new SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter();
                myLocks = new SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter();
                if (!TryDatabaseConnection())
                {
                    MessageBox.Show("I cannot connect to the database", "SmartLock", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Environment.Exit(1);
                }
            }

            textBox1.AppendText("Connected to the database!");
            textBox1.AppendText(Environment.NewLine);

            int? lSeenLog = myLogs.GetMaxLogID();
            if (lSeenLog == null)
                lastSeenLog = 0;
            else
                lastSeenLog = lSeenLog.Value;


            textBox1.AppendText("Starting REST service...");
            textBox1.AppendText(Environment.NewLine);
            SmartLockRESTService myRESTService = new SmartLockRESTService();
            WebServiceHost _serviceHost = new WebServiceHost(myRESTService, new Uri("http://localhost:8000/SmartLockRESTService"));
            _serviceHost.Open();
            textBox1.AppendText("REST service started!");
            textBox1.AppendText(Environment.NewLine);

            textBox1.AppendText("Starting Web Host service...");
            textBox1.AppendText(Environment.NewLine);
            WebServiceHost myWebHost = new WebServiceHost(new NancyWcfGenericService(), new Uri("http://localhost"));
            myWebHost.AddServiceEndpoint(typeof(NancyWcfGenericService), new WebHttpBinding(), "");
            myWebHost.Open();
            textBox1.AppendText("Web Host service started!");
            textBox1.AppendText(Environment.NewLine);


            logThread = new Thread(updateTextBoxWithLogs);
            logThread.Start();

            checkLocksThread = new Thread(CheckOfflineLocks);
            checkLocksThread.Start();

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") System Started!", DateTime.Now, 2, 0);

            //base.OnLoad(e);
        }

        private bool TryDatabaseConnection() {
            try
            {
               
                myAdmin.Fill(myDataSet.Table_Admin);
                myLogs.Fill(myDataSet.Table_Log);
                return true;
            }
            catch (Exception e)
            {
                
            }
            return false;
        }


        private async void updateTextBoxWithLogs()
        {
            string[] lines = { "", "" };
            Telegram.Bot.Types.User me = null;

            var botoffset = 0;
            
            while (true)
                {

                string[] newlines = System.IO.File.ReadAllLines("BotTelegram.txt");

                BotEnabled = Int32.Parse(newlines[1]);
                if (BotEnabled == 1)
                {
                    try
                    {
                        if (newlines[0] != lines[0])
                        {
                            System.Console.WriteLine("Updating Bot Token");
                            lines[0] = newlines[0];
                            Bot = new Telegram.Bot.TelegramBotClient(lines[0]);

                            me = await Bot.GetMeAsync();
                            System.Console.WriteLine("Hello my name is " + me.FirstName);
                        
                        }
                        var updates = await Bot.GetUpdatesAsync(botoffset);
                        foreach (var update in updates)
                        {
                            switch (update.Type)
                            {
                                case Telegram.Bot.Types.Enums.UpdateType.MessageUpdate:
                                    var message = update.Message;

                                    switch (message.Type)
                                    {
                                        case Telegram.Bot.Types.Enums.MessageType.TextMessage:

                                            if (message.Text == "/start")
                                            {
                                                var rkm = new ReplyKeyboardMarkup(new KeyboardButton[] { new KeyboardButton("Send Phone Number") { RequestContact = true } }, true, true);
                                                await Bot.SendTextMessageAsync(message.Chat.Id, "Hello! I'm " + me.FirstName + ".\nSend your contact in order to register on the system", replyToMessageId: message.MessageId, replyMarkup: rkm);
                                            }
                                            if (message.Text == "/stop")
                                            {
                                                myAdmin.UpdateChatIdByChatId(null, message.Chat.Id);
                                                await Bot.SendTextMessageAsync(message.Chat.Id, "I've removed your registration", replyToMessageId: message.MessageId);
                                            }
                                            break;
                                        case Telegram.Bot.Types.Enums.MessageType.ContactMessage:
                                            EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> madmins = myAdmin.GetDataByPhoneNumber("+" + message.Contact.PhoneNumber).AsEnumerable();
                                            if (madmins == null)
                                            {
                                                await Bot.SendTextMessageAsync(message.Chat.Id, "I didn't find your number");
                                                break;
                                            }
                                            if (madmins.Count() == 0)
                                            {
                                                await Bot.SendTextMessageAsync(message.Chat.Id, "I didn't find your number");
                                                break;
                                            }
                                            Console.WriteLine("Contact Received: " + message.Contact.PhoneNumber);
                                            myAdmin.UpdateChatId(message.Chat.Id, madmins.ElementAt(0).AdminID);
                                            await Bot.SendTextMessageAsync(message.Chat.Id, "I've received your account, now you will receive the selected logs");
                                            break;
                                    }
                                    break;
                            }
                            botoffset = update.Id + 1;
                        }
                        BotErrorSegnalated = 0;
                    }
                    catch (Exception e)
                    {
                        if(BotErrorSegnalated==0)
                            myLogs.Insert("[System: Error] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Cannot connect to Telegram", DateTime.Now, 4, 0);
                        BotErrorSegnalated = 1;
                    }
                }



                Thread.Sleep(500);
                this.updatemyTextBoxWithLogs();
            }
        }

        delegate void SetTextCallback();
        private async void updatemyTextBoxWithLogs()
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.textBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(updatemyTextBoxWithLogs);
                this.Invoke(d, new object[] { });
            }
            else
            {
                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LogRow> myNewLogs = myLogs.GetLogsHigherThan(lastSeenLog).AsEnumerable();
                if (myNewLogs == null)
                    return;
                if (myNewLogs.Count() == 0)
                    return;

                if (BotEnabled == 1)
                {
                    EnumerableRowCollection<SmartLockDatabaseDataSet.Table_AdminRow> myadmins = myAdmin.GetData().AsEnumerable();
                    foreach (SmartLockDatabaseDataSet.Table_AdminRow myadmin in myadmins)
                    {
                        if (!myadmin.IsAdminChatIdNull())
                        {
                            foreach (SmartLockDatabaseDataSet.Table_LogRow mlog in myNewLogs)
                            {
                                if ((mlog.LogType & myadmin.AdminLogType) != 0)
                                {
                                    try
                                    {
                                        await Bot.SendTextMessageAsync(myadmin.AdminChatId, mlog.LogText);
                                        BotErrorSegnalated = 0;
                                    }
                                    catch (Telegram.Bot.Exceptions.ApiRequestException e)
                                    {
                                        if (e.Message == "Forbidden: bot was blocked by the user")
                                        {
                                            myAdmin.UpdateChatId(null, myadmin.AdminID);
                                            System.Console.WriteLine("chat id deleted");
                                        }
                                    }
                                    catch (Exception e) {
                                        if (BotErrorSegnalated == 0)
                                            myLogs.Insert("[System: Error] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Cannot connect to Telegram", DateTime.Now, 4, 0);
                                        BotErrorSegnalated = 1;
                                    }
                                }
                            }
                        }
                    }
                }


                foreach (SmartLockDatabaseDataSet.Table_LogRow mlog in myNewLogs)
                {
                    textBox1.AppendText(mlog.LogText);
                    textBox1.AppendText(Environment.NewLine);
                }

                lastSeenLog=myNewLogs.Last().LogID;

            }
        }


        private void CheckOfflineLocks()
        {
            while (true)
            {
                Thread.Sleep(5000);
                EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> myLockList = myLocks.GetData().AsEnumerable();
                for(int i=0; i<errorLocks.Count;i++)
                {
                    int? p = searchlockID(errorLocks.ElementAt(i)[0], myLockList);
                    if (!p.HasValue)
                    {
                        errorLocks.RemoveAt(i);
                        i = -1;
                    }
                }
                foreach (SmartLockDatabaseDataSet.Table_LocksRow mylock in myLockList)
                {
                    int? p = searchlockID(mylock.LockID, errorLocks);
                    DateTime dt = (mylock.IsLockLastSeenNull()) ? mylock.LockRegistrationDate : mylock.LockLastSeen;
                    DateTime now = DateTime.Now;
                    if (!p.HasValue)
                    {
                        int t = 0;
                        if (dt.AddMinutes(mylock.LockMinutesOffline) < now)
                            t = (int)(now - dt).TotalMinutes;
                        errorLocks.Add(new int[2] { mylock.LockID, t });
                        p = searchlockID(mylock.LockID, errorLocks);
                    }

                    if (dt.AddMinutes(mylock.LockMinutesOffline + errorLocks.ElementAt((int)p)[1]) < now)
                    {
                        errorLocks.ElementAt((int)p)[1] += mylock.LockMinutesOffline;
                        myLogs.Insert("[System: Error] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Lock "+ ((mylock.IsLockNameNull())? mylock.LockID.ToString(): mylock.LockName) + " is offline from "+ errorLocks.ElementAt((int)p)[1] + " minutes", DateTime.Now, 4, 0);
                    } else if (dt.AddMinutes(mylock.LockMinutesOffline) >= now)
                    {
                        if(errorLocks.ElementAt((int)p)[1]!=0)
                            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") Lock " + ((mylock.IsLockNameNull()) ? mylock.LockID.ToString() : mylock.LockName) + " is back online!", DateTime.Now, 4, 0);
                        errorLocks.ElementAt((int)p)[1] = 0;
                    }

                }

            }
        }

        private int? searchlockID(int id, List<int[]> mylist)
        {
            for(int i=0; i< mylist.Count; i++)
            {
                if (mylist.ElementAt(i)[0] == id)
                    return i;
            }
            return null;
        }

        private int? searchlockID(int id, EnumerableRowCollection<SmartLockDatabaseDataSet.Table_LocksRow> mylist)
        {
            for (int i = 0; i < mylist.Count(); i++)
            {
                if (mylist.ElementAt(i).LockID == id)
                    return i;
            }
            return null;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }



        private void Form1_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Determine if text has changed in the textbox by comparing to original text.
            // Display a MsgBox asking the user to save changes or abort.
            if (MessageBox.Show("Are you sure?", "SmartLock",
                MessageBoxButtons.YesNo) == DialogResult.No)
            {
                // Cancel the Closing event from closing the form.
                e.Cancel = true;
            }
            else
            {
                logThread.Abort();
                checkLocksThread.Abort();
            }
           
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://localhost");
        }


    }
}
