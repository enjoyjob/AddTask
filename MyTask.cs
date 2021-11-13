using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AddTask
{
    public class MyTask
    {
        public event EventHandler<TaskAcceptArgs> TaskAcceptEvent;
        public event EventHandler<TaskChangeArgs> TaskChangeEvent;

        private const int CheckTaskInterval_ms = 20; // ms
        private const int DefaultMaxRun = 4;
        private const int DefaultMaxWait = 6;
        private volatile bool IsTaskCanceled = false; //flag for cancel task
        private readonly SortedDictionary<int, TaskInfo> TaskDict;

        private volatile int _LastTaskID = 0;

        private static readonly object _lockObj_Task = new object();
        private string _InfoResults = "";
        private string InfoResults
        {
            //get
            //{
            //    return _InfoResults;
            //}
            set
            {
                if (value == _InfoResults)
                {
                    return;
                }
                _InfoResults = value;
                TaskChangeEvent?.Invoke(this, new TaskChangeArgs(_InfoResults));
            }
        }

        private readonly int _MaxRun = 0;
        private readonly int _MaxWait = 0;

        private enum MyTaskStatus
        {
            waiting = 0,
            running,
            end,
        }

        private class TaskInfo
        {
            public int index = 0;
            public MyTaskStatus status = MyTaskStatus.waiting;
            public int progress = 0;

            override public string ToString()
            {
                return $"{index},{status},{progress}%";
            }
        }

        //event args for task acceptable
        public class TaskAcceptArgs : EventArgs
        {
            public TaskAcceptArgs()
            {
            }
        }

        //event args for task changed
        public class TaskChangeArgs : EventArgs
        {
            private readonly string _InfoResults = "";
            public TaskChangeArgs(string InfoResults)
            {
                this._InfoResults = InfoResults;
            }

            //
            // summary:
            //     return tasks's info result
            //
            // param:
            //   InfoResults:
            //     format "index,status,progress|index,status,progress|..."
            //     such as "1,running,10"
            //     such as "1,running,50|2,running,45|3,running,20|4,running,5|5,waiting,0|6,waiting,0"
            //     such as ""
            public string InfoResults
            {
                get { return _InfoResults; }
            }
        }
        public MyTask() : this(DefaultMaxRun, DefaultMaxWait)
        {
        }

        public MyTask(int MaxRun, int MaxWait)
        {
            _MaxRun = MaxRun < 1 ? DefaultMaxRun : MaxRun;
            _MaxWait = MaxWait < 1 ? DefaultMaxWait : MaxWait;
            TaskDict = new SortedDictionary<int, TaskInfo>();

            Task.Run(() => CheckingTask());
        }

        public bool AddTask()
        {
            if (IsTaskCanceled)
            {
                return false;
            }

            lock (_lockObj_Task)
            {
                var keysWaiting = GetKeys(MyTaskStatus.waiting);
                if (keysWaiting.Count < _MaxWait)
                {
                    _LastTaskID++;
                    TaskDict.Add( _LastTaskID, new TaskInfo() { index = _LastTaskID });
                    return (TaskDict.Count < _MaxWait);
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsTaskRunning()
        {
            var keysRunning = GetKeys(MyTaskStatus.running);
            return keysRunning.Count != 0;
        }
        public void CancelTask()
        {
            IsTaskCanceled = true;

            lock (_lockObj_Task)
            {
                TaskDict.Clear();
            }
            
            Thread.Sleep(100);

            _LastTaskID = 0;
        }

        const int Task_Step_Time_ms = 10 * 1000 / 100;
        private void RunWaitingTask(TaskInfo taskInfo)
        {
            if (taskInfo.status != MyTaskStatus.waiting)
            {
                return;
            }
            taskInfo.status = MyTaskStatus.running;
            for (int i = 0; i <= 100; i++)
            {
                taskInfo.progress = i;
                if (!UpdateTaskInfo(taskInfo))
                {
                    return;
                }

                //do some thing,current just sleep
                Thread.Sleep(Task_Step_Time_ms);
            }

            //status change to end
            taskInfo.status = MyTaskStatus.end;
            if (!UpdateTaskInfo(taskInfo))
            {
                return;
            }
            Thread.Sleep(CheckTaskInterval_ms);
        }

        private void CheckingTask()
        {
            while (!IsTaskCanceled)
            {
                lock (_lockObj_Task)
                {
                    List<int> keysEnd = GetKeys(MyTaskStatus.end);
                    List<int> keysRunning = GetKeys(MyTaskStatus.running);
                    List<int> keysWaiting = GetKeys(MyTaskStatus.waiting);

                    //remove end task from TaskDict
                    foreach (var key in keysEnd)
                    {
                        TaskDict.Remove(key);
                    }

                    if(keysWaiting.Count < _MaxWait)
                    {
                        TaskAcceptEvent?.Invoke(this, new TaskAcceptArgs());
                    }

                    //run waiting task
                    if(keysWaiting.Count > 0)
                    {
                        int iMin = Math.Min(_MaxRun - keysRunning.Count, keysWaiting.Count);
                        for (int i = 0; i < iMin; i++)
                        {
                            var taskInfo = TaskDict[keysWaiting[i]];
                            Task.Run(() =>
                            {
                                RunWaitingTask(taskInfo);

                            });
                        }
                    }


                    //update InfoResults
                    InfoResults = GetInfoResults();
                }

                //wait a short time
                Thread.Sleep(CheckTaskInterval_ms);
            }
        }

        private List<int> GetKeys(MyTaskStatus myTaskStatus)
        {
            List<int> ret = new List<int>();

            var keys = TaskDict.Keys;
            foreach (var key in keys)
            {
                if (TaskDict[key].status == myTaskStatus)
                {
                    ret.Add(key);
                }
            }
            return ret;
        }
        private string GetInfoResults()
        {
            string ret;

            //   InfoResults:
            //     format "index,status,progress|index,status,progress|..."
            //     such as "1,running,10"
            //     such as "1,running,50|2,running,45|3,running,20|4,running,5|5,waiting,0|6,waiting,0"
            //     such as ""
            List<int> keysRunning = GetKeys(MyTaskStatus.running);
            List<int> keysWaiting = GetKeys(MyTaskStatus.waiting);

            List<string> lstTask = new List<string>();

            //running
            foreach(var key in keysRunning)
            {
                if (TaskDict.ContainsKey(key))
                {
                    lstTask.Add(TaskDict[key].ToString());
                }
            }
            //waiting
            foreach (var key in keysWaiting)
            {
                if (TaskDict.ContainsKey(key))
                {
                    lstTask.Add(TaskDict[key].ToString());
                }
            }

            ret = string.Join("|", lstTask);
            return ret;
        }

        private bool UpdateTaskInfo(TaskInfo taskInfo)
        {
            lock (_lockObj_Task)
            {
                if (TaskDict.ContainsKey(taskInfo.index))
                {
                    TaskDict[taskInfo.index] = taskInfo;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
