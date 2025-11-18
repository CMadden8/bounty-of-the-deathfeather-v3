using System.Collections.Generic;
using System.Threading.Tasks;
using TurnBasedStrategyFramework.Common.Cells;
using TurnBasedStrategyFramework.Common.Units;
using TurnBasedStrategyFramework.Unity.Utilities;
using UnityEngine;

namespace TurnBasedStrategyFramework.Unity.Units.Abilities
{
    /// <summary>
    /// A Unity-specific implementation of the <see cref="MoveComponent"/> responsible for handling unit movement animations in the game.
    /// </summary>
    public class UnityMoveComponent : MoveComponent
    {
        public UnityMoveComponent(IUnit unitReference) : base(unitReference)
        {
        }

        public override async Task MovementAnimation(IEnumerable<ICell> path, ICell destination)
        {
            var currentCell = _unitReference.CurrentCell;
            // Preserve vertical offset between unit and cell so unit doesn't sink into the tile
            var unitPos = _unitReference.WorldPosition.ToVector3();
            var cellPos = currentCell?.WorldPosition.ToVector3() ?? Vector3.zero;
            var yOffset = unitPos.y - cellPos.y;
            
            foreach (var cell in path)
            {
                _unitReference.InvokeUnitLeftCell(new UnitChangedGridPositionEventArgs(_unitReference, currentCell, cell));

                // Calculate cell center position. ICell (common) doesn't expose CellDimensions,
                // so try casting to the Unity Cell type and fall back to a 0.5 offset if unavailable.
                Vector3 centerOffset = new Vector3(0.5f, 0f, 0.5f);
                if (cell is TurnBasedStrategyFramework.Unity.Cells.Cell unityCell)
                {
                    centerOffset = new Vector3(unityCell.CellDimensions.x * 0.5f, 0f, unityCell.CellDimensions.z * 0.5f);
                }

                var cellCenter = cell.WorldPosition.ToVector3() + centerOffset;
                var targetPos = cellCenter + new Vector3(0f, yOffset, 0f);

                while (!_unitReference.WorldPosition.ToVector3().Equals(targetPos))
                {
                    var newPos = Vector3.MoveTowards(_unitReference.WorldPosition.ToVector3(), targetPos, Time.deltaTime * _unitReference.MovementAnimationSpeed);
                    _unitReference.WorldPosition = newPos.ToIVector3();
                    await Awaitable.NextFrameAsync();
                }

                _unitReference.InvokeUnitEnteredCell(new UnitChangedGridPositionEventArgs(_unitReference, currentCell, cell));
            }

            // Finalize at destination center with preserved vertical offset
            Vector3 destCenterOffset = new Vector3(0.5f, 0f, 0.5f);
            if (destination is TurnBasedStrategyFramework.Unity.Cells.Cell destUnityCell)
            {
                destCenterOffset = new Vector3(destUnityCell.CellDimensions.x * 0.5f, 0f, destUnityCell.CellDimensions.z * 0.5f);
            }
            var destCenter = destination.WorldPosition.ToVector3() + destCenterOffset;
            var destPos = destCenter + new Vector3(0f, yOffset, 0f);
            _unitReference.WorldPosition = destPos.ToIVector3();
        }
    }
}