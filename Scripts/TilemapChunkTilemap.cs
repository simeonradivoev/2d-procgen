using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    [RequireComponent(typeof(Tilemap))]
    public class TilemapChunkTilemap : MonoBehaviour
    {
        public TilemapChunkTilemap[] Neighbors { get; } = new TilemapChunkTilemap[9];

        public Tilemap Tilemap { get; set; }

        public int2 Size { get; set; }

        private void Awake()
        {
            Tilemap = GetComponent<Tilemap>();
        }
    }
}