using System;
using System.Windows.Forms;

namespace AddTask
{
    public partial class FrmTask : Form
    {
        readonly MyTask myTask = new MyTask();
        public FrmTask()
        {
            InitializeComponent();
            MyInit();
        }
        private void MyInit()
        {
            //form 设定
            Text = "Add Task";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            StartPosition = FormStartPosition.CenterScreen;
            MaximizeBox = false;
            MinimizeBox = false;

            FormClosing += MyFormClosing;

            myTask.TaskChangeEvent += OnTaskChange; //订阅TaskChange事件
            myTask.TaskAcceptEvent += OnTaskAcceptable; //订阅TaskAccept事件
            
            //DataGridView
            dgvTask.RowHeadersVisible = false;
            dgvTask.AllowUserToAddRows = false;
            dgvTask.AllowUserToDeleteRows = false;
            dgvTask.ReadOnly = true;
            dgvTask.MultiSelect = false;
            dgvTask.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            //dgvTask.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            //foreach (DataGridViewColumn item in dgvTask.Columns) item.SortMode = DataGridViewColumnSortMode.NotSortable;

        }

        private void MyFormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = false; //close form

            //task running
            if (myTask.IsTaskRunning())
            {
                //comfirm to close
                if (DialogResult.Yes == MessageBox.Show("task running, sure to close?", "by quanlingming",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2))
                {
                    myTask.TaskChangeEvent -= OnTaskChange;
                    myTask.CancelTask();
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }

        private void BtnAddTask_Click(object sender, EventArgs e)
        {
            RunTask();
        }

        private void RunTask()
        {
            if (!myTask.AddTask())
            {
                BtnAddTask.Enabled = false;
            }
        }

        //接受事件
        void OnTaskChange(object sender, MyTask.TaskChangeArgs e)
        {
            BindingSource bs = new BindingSource
            {
                DataSource = typeof(Custom)
            };
            //e.Result format:  "1,running,50|2,running,45|3,running,20|4,running,5|5,waiting,0|6,waiting,0"
            var results = e.InfoResults;
            if (dgvTask.InvokeRequired)
            {
                dgvTask.BeginInvoke(new MethodInvoker(delegate
                {
                    if (!string.IsNullOrEmpty(results))
                    {
                        string[] sTaskArray = results.Split('|'); //split to task
                        foreach (var sTask in sTaskArray)
                        {
                            string[] sTaskInfoArray = sTask.Split(','); //split to task's info
                            if (sTaskInfoArray.Length < 3)
                            {
                                continue;
                            }
                            bs.Add(new Custom(sTaskInfoArray[0], sTaskInfoArray[1], sTaskInfoArray[2]));
                        }
                    }
                    dgvTask.DataSource = bs;//赋值控件自动更新

                }));
            }
            else
            {
            }

        }

        void OnTaskAcceptable(object sender, MyTask.TaskAcceptArgs e)
        {
            if (BtnAddTask.InvokeRequired)
            {
                BtnAddTask.BeginInvoke(new MethodInvoker(delegate
                {
                    BtnAddTask.Enabled = true;
                }));
            }
            else
            {
                BtnAddTask.Enabled = true;
            }
        }

    }

    public class Custom
    {
        private string _index;
        private string _status;
        private string _progress;

        public string Index
        {
            get { return _index; }
            set { _index = value; }
        }

        public string Status
        {
            get { return _status; }
            set { _status = value; }
        }

        public string Progress
        {
            get { return _progress; }
            set { _progress = value; }
        }

        public Custom()
        { }
        public Custom(string index, string status, string progress)
        {
            this._index = index;
            this._status = status;
            this._progress = progress;
        }

    }
}
