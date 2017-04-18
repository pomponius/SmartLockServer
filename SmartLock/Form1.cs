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
        Thread newThread;
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


            newThread = new Thread(updateTextBoxWithLogs);
            newThread.Start();


            myLogs.Insert("[System: Info] (" + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) + ") System Started!", DateTime.Now, 2, 0);
            //textBox1.AppendText("("+DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture) +") System Started!");
            //textBox1.AppendText(Environment.NewLine);
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
                newThread.Abort();
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
