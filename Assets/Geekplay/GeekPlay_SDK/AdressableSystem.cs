using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using UnityEngine.AddressableAssets;
// using UnityEngine.ResourceManagement.AsyncOperations;

public class AdressableSystem : MonoBehaviour
{
    // [SerializeField] private AssetReferenceGameObject gameObject;
    GameObject _instanceObject;

    void Start()
    {
    	//if (Input.GetKeyDown(KeyCode.Space))
    	//{
    		// gameObject.InstantiateAsync().Completed += OnAddressableLoaded;
    	//}
    	//else if (Input.GetKeyDown(KeyCode.D))
    	//{
    	//	gameObject.ReleaseInstance(_instanceObject);
    	//}
    }

    // void OnAddressableLoaded(AsyncOperationHandle<GameObject> handle)
    // {
    // 	if (handle.Status == AsyncOperationStatus.Succeeded)
    // 	{
    // 		_instanceObject = handle.Result;
    // 	}
    // 	else
    // 	{
    // 		Debug.Log("Load fail!");
    // 	}
    // }
}
