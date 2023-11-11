using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterFactory.TaskManagement
{
    [Serializable]
    public class PlayerTaskData
    {
        public string OnCompleteFeedbackId;
        public string OnAbortFeedbackId;
        public string OnFailedFeedbackId;
        public string OnStartedFeedbackId;
        public List<string> SubTaskIds;
    }

    public enum TaskState
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Aborted = 4
    }
    

    public abstract class PlayerTaskProgress
    {
        public string taskId;
        public TaskState taskState;
    }

    public interface ITaskDataProvider
    {
        public PlayerTaskData GetTaskDataById(string id);
        public string GetTaskOnCompleteFeedbackById(string id);
        public string GetTaskOnAbortFeedbackById(string id);
        public string GetTaskOnFailedFeedbackById(string id);
        public string GetTaskOnStartedFeedbackById(string id);
        public List<string> GetSubTasksForTaskId(string id);
        public List<string> GetAllTaskIds();
        public List<PlayerTaskData> GetAllTasks();
    }
}
