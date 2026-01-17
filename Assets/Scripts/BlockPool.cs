using UnityEngine;
using System.Collections.Generic;

public class BlockPool : MonoBehaviour
{
    public static BlockPool Instance { get; private set; }

    [SerializeField] private GameObject blockPrefab;
    private Queue<BlockView> _pool = new Queue<BlockView>();
    private Transform _poolContainer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        _poolContainer = new GameObject("PoolContainer").transform;
        _poolContainer.SetParent(transform);
    }

    public void InitializePool(int initialPoolSize)
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewBlock();
        }
    }

    private BlockView CreateNewBlock()
    {
        GameObject obj = Instantiate(blockPrefab, _poolContainer);
        BlockView view = obj.GetComponent<BlockView>();
        obj.SetActive(false);
        _pool.Enqueue(view);
        return view;
    }

    public BlockView GetBlock()
    {
        if (_pool.Count == 0)
        {
            CreateNewBlock();
        }

        BlockView view = _pool.Dequeue();
        view.gameObject.SetActive(true);
        return view;
    }

    public void ReturnBlock(BlockView view)
    {
        view.gameObject.SetActive(false);
        view.transform.SetParent(_poolContainer); // Keep hierarchy clean
        view.transform.localScale = Vector3.one; // Reset scale
        _pool.Enqueue(view);
    }
}