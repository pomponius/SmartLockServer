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

namespace SmartLock
{
    public partial class Form1 : Form
    {
        SmartLockDatabaseDataSet myDataSet = new SmartLockDatabaseDataSet();
        SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter myAdmin = new SmartLockDatabaseDataSetTableAdapters.Table_AdminTableAdapter();
        SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter myLogs = new SmartLockDatabaseDataSetTableAdapters.Table_LogTableAdapter();
        SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter myLocks = new SmartLockDatabaseDataSetTableAdapters.Table_LocksTableAdapter();
        Thread logThread;
        Thread checkLocksThread;
        List<int[]> errorLocks = new List<int[]>();
        int lastSeenLog = 0;


        public Form1()
        {
            InitializeComponent();
            this.FormClosing += new FormClosingEventHandler(Form1_Closing);

            myAdmin.Fill(myDataSet.Table_Admin);
            myLogs.Fill(myDataSet.Table_Log);

            int? lSeenLog = myLogs.GetMaxLogID();
            if(lSeenLog == null)
                lastSeenLog = 0;
            else
                lastSeenLog = lSeenLog.Value;

            

            SmartLockRESTService myRESTService = new SmartLockRESTService();
            WebServiceHost _serviceHost = new WebServiceHost(myRESTService, new Uri("http://localhost:8000/SmartLockRESTService"));
            _serviceHost.Open();


            WebServiceHost myWebHost = new WebServiceHost(new NancyWcfGenericService(), new Uri("http://localhost"));
            myWebHost.AddServiceEndpoint(typeof(NancyWcfGenericService), new WebHttpBinding(), "");
            myWebHost.Open();


            logThread = new Thread(updateTextBoxWithLogs);
            logThread.Start();

            checkLocksThread = new Thread(CheckOfflineLocks);
            checkLocksThread.Start();

            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") System Started!", DateTime.Now, 2, 0);
        }

        private void updateTextBoxWithLogs()
        {
            while(true)
                {
                Thread.Sleep(500);
                this.updatemyTextBoxWithLogs();
            }
        }

        delegate void SetTextCallback();
        private void updatemyTextBoxWithLogs()
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
                
                foreach(SmartLockDatabaseDataSet.Table_LogRow mlog in myNewLogs)
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
