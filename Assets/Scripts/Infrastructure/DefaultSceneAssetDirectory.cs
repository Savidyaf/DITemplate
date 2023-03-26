using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


public interface ISceneAssetDirectory
{
    public List<AsyncOperationHandle> GetScenePreloadAssetReferences();
}
