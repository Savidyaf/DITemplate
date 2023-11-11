using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MessagePipe;
using MonsterFactory.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using VContainer;

namespace MonsterFactory.TaskManagement
{
    /*
     * Make sure to import UnityEngine from any inheriting scripts.
     */
    public abstract class PlayerTaskFeedbackWorldProcessor : MonoBehaviour
    {
        private IDisposable eventDisposableBag;
        private ITaskDataProvider playerTaskDataProvider;

        public UnityEvent<string> TaskStarted;
        public UnityEvent<string> TaskFailed;
        public UnityEvent<string> TaskAborted;
        public UnityEvent<string> TaskCompleted;

        [Inject]
        public void InjectTaskEvents(IScopedAsyncSubscriber<PlayerTaskBaseEvent> taskEventSubscriber, ITaskDataProvider playerTaskDataProvider)
        {
            DisposableBagBuilder disposableBagBuilder = DisposableBag.CreateBuilder();
            this.playerTaskDataProvider = playerTaskDataProvider;
            taskEventSubscriber
                .Subscribe(
                    (playerEvent, token) => HandlePlayerTaskEvent(playerEvent, token).SuppressCancellationThrow())
                .AddTo(disposableBagBuilder);
            eventDisposableBag = disposableBagBuilder.Build();
        }

        private void OnDestroy()
        {
            //Dispose all event subscriptions
            eventDisposableBag.Dispose();
        }

        private async UniTask HandlePlayerTaskEvent(PlayerTaskBaseEvent playerTaskBaseEvent,
            CancellationToken cancellationToken)
        {
            var eventId = playerTaskBaseEvent.Id;
            if (playerTaskBaseEvent is PlayerTaskStarted started)
            {
                TaskStarted?.Invoke(playerTaskDataProvider.GetTaskOnStartedFeedbackById(eventId));
                await OnTaskStarted(started, cancellationToken);
                return;
            }

            if (playerTaskBaseEvent is PlayerTaskAborted aborted)
            {
                TaskAborted?.Invoke(playerTaskDataProvider.GetTaskOnAbortFeedbackById(eventId));
                await OnTaskAborted(aborted, cancellationToken);
                return;
            }

            if (playerTaskBaseEvent is PlayerTaskFailed failed)
            {
                TaskFailed?.Invoke(playerTaskDataProvider.GetTaskOnFailedFeedbackById(eventId));
                await OnTaskFailed(failed, cancellationToken);
                return;
            }

            if (playerTaskBaseEvent is PlayerTaskCompleted completed)
            {
                TaskCompleted?.Invoke(playerTaskDataProvider.GetTaskOnCompleteFeedbackById(eventId));
                await OnTaskCompleted(completed, cancellationToken);
                return;
            }
        }

        protected abstract UniTask OnTaskStarted(PlayerTaskStarted started, CancellationToken cancellationToken);
        protected abstract UniTask OnTaskCompleted(PlayerTaskCompleted completed, CancellationToken cancellationToken);
        protected abstract UniTask OnTaskAborted(PlayerTaskAborted aborted, CancellationToken cancellationToken);
        protected abstract UniTask OnTaskFailed(PlayerTaskFailed failed, CancellationToken cancellationToken);
    }
}