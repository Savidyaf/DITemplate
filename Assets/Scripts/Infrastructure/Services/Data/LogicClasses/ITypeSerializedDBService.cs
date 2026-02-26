using System.Threading;
using Cysharp.Threading.Tasks;

namespace SpiralingStudio.Services.DataManagement
{
    public interface ITypeSerializedDBService
    {
        /// <summary>
        /// Fetches data of type T from the runtime database.
        /// </summary>
        /// <param name="typeCode">The code identifying the type of data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <typeparam name="T">The type of data to fetch.</typeparam>
        /// <returns>A UniTask representing the asynchronous operation.</returns>
        public UniTask<T> FetchDataFromRuntimeDatabase<T>(string typeCode, CancellationToken cancellationToken) where T : MFSaveData;

        /// <summary>
        /// Writes data of type T to the runtime database.
        /// </summary>
        /// <typeparam name="T">The type of data to write.</typeparam>
        /// <param name="typeCode">The code identifying the type of data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <param name="dataInstance">The instance of data to write.</param>
        /// <returns>True : If write operation succeeded, False : Write operation failed </returns>
        public UniTask<bool> WriteDataToRuntimeDatabase<T>(string typeCode, CancellationToken cancellationToken, T dataInstance)
            where T : MFSaveData;
    }
}