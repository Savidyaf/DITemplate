using System;
using System.Collections;
using MessagePipe;
using MonsterFactory.Events;
using MonsterFactory.Services;
using MonsterFactory.Services.DataManagement;
using UnityEngine;

namespace MonsterFactory.TaskManagement
{
    public class TaskDataManager : MFService
    {
        private readonly IDataConnector dataConnector;
        private readonly IAsyncPublisher<PlayerTaskBaseEvent> taskEventPublisher;

        public TaskDataManager(IDataConnector dataConnector, IAsyncPublisher<PlayerTaskBaseEvent> taskEventPublisher)
        {
            this.dataConnector = dataConnector;
            this.taskEventPublisher = taskEventPublisher;
        }

        #region API

        public TaskState GetTaskProgressState(string id)
        {
            if (dataConnector.GetTaskProgressById(id) is { } progress)
            {
                return progress.taskState;
            }

            return TaskState.NotStarted;
        }

        public PlayerTaskProgress GetPlayerTaskProgressById(string id)
        {
            if (dataConnector.GetTaskProgressById(id) is { } progress)
            {
                return progress;
            }

            return null;
        }

        public void UpdatePlayerTaskProgress(string id, PlayerTaskProgress progress)
        {
            bool shouldPublishState = CheckForStateChange(id, progress);
            dataConnector.SetTaskProgressById(id, progress);
            if (shouldPublishState)
            {
                taskEventPublisher.Publish(ResolveStateEvent(progress));
            }
            
        }

        #endregion


        #region Implementation

        private bool CheckForStateChange(string id, PlayerTaskProgress progress)
        {
            return dataConnector.GetTaskProgressById(id)?.taskState != progress.taskState;
        }

        private PlayerTaskBaseEvent ResolveStateEvent(PlayerTaskProgress progress)
        {
            switch (progress.taskState)
            {
                case TaskState.InProgress:
                    return new PlayerTaskStarted(progress.taskId);
                case TaskState.Completed:
                    return new PlayerTaskCompleted(progress.taskId);
                case TaskState.Failed:
                    return new PlayerTaskFailed(progress.taskId);
                case TaskState.Aborted:
                    return new PlayerTaskAborted(progress.taskId);
                case TaskState.NotStarted:
                default:
                    return null;
            }
        }

        #endregion
    }
}
