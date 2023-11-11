using System;
using System.Collections.Generic;
using System.Linq;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace MonsterFactory.TaskManagement
{
    [CreateAssetMenu(menuName = "TaskData/TaskDataProvider", fileName = "PlayerTaskDataProvider", order = 0)]
    public class PlayerTaskDataProvider : ScriptableObject,ITaskDataProvider
    { 
        [SerializedDictionary("ID", "TaskData")]
        public SerializedDictionary<string, PlayerTaskData> TaskDataById;


        public PlayerTaskData GetTaskDataById(string id)
        {
            if (TaskDataById.TryGetValue(id, out PlayerTaskData data))
            {
                return data;
            }
            return null;
        }

        public string GetTaskOnCompleteFeedbackById(string id)
        {
            return GetTaskDataById(id)?.OnCompleteFeedbackId;
        }

        public string GetTaskOnAbortFeedbackById(string id)
        {
            return GetTaskDataById(id)?.OnAbortFeedbackId;
        }

        public string GetTaskOnFailedFeedbackById(string id)
        {
            return GetTaskDataById(id)?.OnFailedFeedbackId;
        }

        public string GetTaskOnStartedFeedbackById(string id)
        {
            return GetTaskDataById(id).OnStartedFeedbackId;
        }

        public List<string> GetSubTasksForTaskId(string id)
        {
            return GetTaskDataById(id)?.SubTaskIds;
        }

        public List<string> GetAllTaskIds()
        {
            return TaskDataById.Keys.ToList();
        }

        public List<PlayerTaskData> GetAllTasks()
        {
            return TaskDataById.Values.ToList();
        }
    }
}