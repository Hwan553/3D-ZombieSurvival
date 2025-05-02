
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager instance;

    [SerializeField]
    private GameObject[] prefabs;

    [SerializeField]
    private int poolSize = 1;

    private List<GameObject>[] objPools;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitObjPool();
    }

    private void InitObjPool()
    {
        objPools = new List<GameObject>[prefabs.Length];

        for (int i = 0; i < prefabs.Length; i++)
        {
            objPools[i] = new List<GameObject>();

            for (int j = 0; j < poolSize; j++)
            {
                GameObject obj = Instantiate(prefabs[i]);
                obj.SetActive(false);
                objPools[i].Add(obj);
            }
        }
    }

    public GameObject ActivateObj(int index)
    {
        GameObject obj = null;

        for (int i = 0; i < objPools[index].Count; i++)
        {
            if (objPools[index][i] == null)
            {
                objPools[index].RemoveAt(i);  // 리스트에서 삭제된 오브젝트 제거
                i--;
                continue;
            }

            if (!objPools[index][i].activeInHierarchy)
            {
                obj = objPools[index][i];
                obj.SetActive(true);
                return obj;
            }
        }

        // 기존 풀에 사용할 수 있는 오브젝트가 없으면 새로 생성
        obj = Instantiate(prefabs[index]);
        objPools[index].Add(obj);
        obj.SetActive(true);

        return obj;
    }
}
