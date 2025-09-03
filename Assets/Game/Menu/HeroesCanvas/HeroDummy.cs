using System;
using Sirenix.OdinInspector;
using UnityEngine;

[Serializable]
public class HeadSkinData
{
    public GameObject skinPrefab;
    public bool needHead;
}

public class HeroDummy : MonoBehaviour
{
    [ShowInInspector] public HeadSkinData[] skinsHead = new HeadSkinData[9];
    [SerializeField] private SkinnedMeshRenderer skinBody;
    [SerializeField] private Material[] materialsBody;

    private void OnValidate()
    {
        if (skinsHead.Length == 0) skinsHead = new HeadSkinData[9];
        // skinBody = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        // skinHead = new GameObject[20];
        // skinBody = new SkinnedMeshRenderer[20];

        // for (int i = 0; i < 20; i++)
        // {
        //     skinBody[i] = transform.GetChild(i).GetComponent<SkinnedMeshRenderer>();
        // }
        // for (int i = 0; i < 20; i++)
        // {
        //     skinHead[i] = transform.GetChild(i+20).gameObject;
        // }
    }

    public void SelectSkin(int id)
    {
        SelectBody(id);
        SelectHead(id);
        // Geekplay.Instance.Save();
    }

    public void SelectHead(int id)
    {
        foreach (var head in skinsHead)
        {
            head.skinPrefab.SetActive(false);
        }

        if (skinsHead[id].needHead == true)
        {
            skinsHead[0].skinPrefab.SetActive(true);
            skinsHead[id].skinPrefab.SetActive(true);
            // skinBody.material = materialsBody[0];
        }
        else
        {
            skinsHead[id].skinPrefab.SetActive(true);
        }

        // Geekplay.Instance.PlayerData.currentHeroHeadSkin = id;
    }

    public void SelectBody(int id)
    {
        skinBody.material = materialsBody[id];

        // Geekplay.Instance.PlayerData.currentHeroBodySkin = id;
    }
}