using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProcGen2D
{
    [CreateAssetMenu(menuName = "2D/Tiles/Procedural Rule Tile", order = 100)]
    public class ProceduralRuleTile : RuleTile
    {
        private TileBase GetNeighborTile(ITilemap tilemap, Vector3Int pos)
        {
            return tilemap.GetTile(pos);
        }

        private TileBase GetTile(ITilemap tilemap, Vector3Int position)
        {
            var procTilemap = tilemap.GetComponent<TilemapChunkTilemap>();
            if (procTilemap)
            {
                int2 dir = 1;

                if (position.x < 0)
                {
                    dir.x = 0;
                }
                else if (position.x >= procTilemap.Size.x)
                {
                    dir.x = 2;
                }

                if (position.y < 0)
                {
                    dir.y = 0;
                }
                else if (position.y >= procTilemap.Size.y)
                {
                    dir.y = 2;
                }

                if (dir.x == 1 && dir.y == 1)
                {
                    return tilemap.GetTile(position);
                }

                var dirIndex = dir.y * 3 + dir.x;
                var neighbor = procTilemap.Neighbors[dirIndex];
                if (neighbor)
                {
                    return GetNeighborTile(
                        neighbor.Tilemap,
                        new Vector3Int((neighbor.Size.x + position.x) % neighbor.Size.x, (neighbor.Size.y + position.y) % neighbor.Size.y, 0));
                }
            }

            return tilemap.GetTile(position);
        }

        private bool RuleMatchesProcedural(TilingRule rule, Vector3Int position, ITilemap tilemap, int angle, bool mirrorX = false)
        {
            var minCount = Math.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
            for (var i = 0; i < minCount; i++)
            {
                var neighbor = rule.m_Neighbors[i];
                var neighborPosition = rule.m_NeighborPositions[i];
                if (mirrorX)
                {
                    neighborPosition = GetMirroredPosition(neighborPosition, true, false);
                }
                var positionOffset = GetRotatedPosition(neighborPosition, angle);
                var other = GetTile(tilemap, GetOffsetPosition(position, positionOffset));
                if (!RuleMatch(neighbor, other))
                {
                    return false;
                }
            }
            return true;
        }

        public bool RuleMatchesProcedural(TilingRule rule, Vector3Int position, ITilemap tilemap, bool mirrorX, bool mirrorY)
        {
            var minCount = Math.Min(rule.m_Neighbors.Count, rule.m_NeighborPositions.Count);
            for (var i = 0; i < minCount; i++)
            {
                var neighbor = rule.m_Neighbors[i];
                var positionOffset = GetMirroredPosition(rule.m_NeighborPositions[i], mirrorX, mirrorY);
                var other = GetTile(tilemap, GetOffsetPosition(position, positionOffset));
                if (!RuleMatch(neighbor, other))
                {
                    return false;
                }
            }
            return true;
        }

        public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
        {
            if (RuleMatchesProcedural(rule, position, tilemap, 0))
            {
                transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, 0f), Vector3.one);
                return true;
            }

            // Check rule against rotations of 0, 90, 180, 270
            if (rule.m_RuleTransform == TilingRuleOutput.Transform.Rotated)
            {
                for (var angle = m_RotationAngle; angle < 360; angle += m_RotationAngle)
                {
                    if (RuleMatchesProcedural(rule, position, tilemap, angle))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                        return true;
                    }
                }
            }
            // Check rule against x-axis, y-axis mirror
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorXY)
            {
                if (RuleMatchesProcedural(rule, position, tilemap, true, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, -1f, 1f));
                    return true;
                }
                if (RuleMatchesProcedural(rule, position, tilemap, true, false))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
                    return true;
                }
                if (RuleMatchesProcedural(rule, position, tilemap, false, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
                    return true;
                }
            }
            // Check rule against x-axis mirror
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorX)
            {
                if (RuleMatchesProcedural(rule, position, tilemap, true, false))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(-1f, 1f, 1f));
                    return true;
                }
            }
            // Check rule against y-axis mirror
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.MirrorY)
            {
                if (RuleMatchesProcedural(rule, position, tilemap, false, true))
                {
                    transform = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1f, -1f, 1f));
                    return true;
                }
            }
            // Check rule against x-axis mirror with rotations of 0, 90, 180, 270
            else if (rule.m_RuleTransform == TilingRuleOutput.Transform.RotatedMirror)
            {
                for (var angle = 0; angle < 360; angle += m_RotationAngle)
                {
                    if (angle != 0 && RuleMatches(rule, position, tilemap, angle))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), Vector3.one);
                        return true;
                    }
                    if (RuleMatchesProcedural(rule, position, tilemap, angle, true))
                    {
                        transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 0f, -angle), new Vector3(-1f, 1f, 1f));
                        return true;
                    }
                }
            }
            return false;
        }
    }
}