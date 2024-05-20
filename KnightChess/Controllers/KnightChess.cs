using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace KnightChess.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KnightPathController : ControllerBase
    {
        private static readonly string DataFilePath = Path.Combine(AppContext.BaseDirectory, "knight_paths.json");

        [HttpPost("knightpath")]
        public IActionResult PostKnightPath([FromQuery] string source, [FromQuery] string target)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(target))
            {
                return BadRequest("Source and target positions are required.");
            }

            if (!IsValidPosition(source) || !IsValidPosition(target))
            {
                return BadRequest("Source and target positions must be valid chess positions (A1 to H8).");
            }

            var operationId = Guid.NewGuid().ToString();
            var pathResult = CalculateKnightPath(source, target);

            var pathData = new KnightPathResponse
            {
                OperationId = operationId,
                Starting = source,
                Ending = target,
                ShortestPath = pathResult.ShortestPath,
                NumberOfMoves = pathResult.NumberOfMoves
            };

            SavePathData(pathData);

            return Ok($"Operation Id {operationId} was created. Please query it to find your results.");
        }

        [HttpGet("knightpath")]
        public IActionResult GetKnightPath([FromQuery] string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId))
            {
                return BadRequest("OperationId is required.");
            }

            var pathData = LoadPathData(operationId);
            if (pathData == null)
            {
                return NotFound("OperationId not found.");
            }

            return Ok(pathData);
        }

        private static (string ShortestPath, int NumberOfMoves) CalculateKnightPath(string start, string end)
        {
            var directions = new (int x, int y)[] { (2, 1), (1, 2), (-1, 2), (-2, 1), (-2, -1), (-1, -2), (1, -2), (2, -1) };
            var queue = new Queue<(string Position, List<string> Path)>();
            var visited = new HashSet<string>();

            queue.Enqueue((start, new List<string> { start }));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (currentPosition, path) = queue.Dequeue();
                var (currentX, currentY) = ChessToCoords(currentPosition);

                if (currentPosition == end)
                {
                    return (string.Join(":", path), path.Count - 1);
                }

                foreach (var (dx, dy) in directions)
                {
                    var nextX = currentX + dx;
                    var nextY = currentY + dy;
                    var nextPosition = CoordsToChess(nextX, nextY);

                    if (IsValidPosition(nextX, nextY) && !visited.Contains(nextPosition))
                    {
                        visited.Add(nextPosition);
                        var newPath = new List<string>(path) { nextPosition };
                        queue.Enqueue((nextPosition, newPath));
                    }
                }
            }

            return (string.Empty, -1);
        }

        private static (int x, int y) ChessToCoords(string position)
        {
            return (position[0] - 'A', position[1] - '1');
        }

        private static string CoordsToChess(int x, int y)
        {
            return $"{(char)('A' + x)}{y + 1}";
        }

        private static bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < 8 && y >= 0 && y < 8;
        }

        private static void SavePathData(KnightPathResponse pathData)
        {
            var pathDataList = LoadAllPathData();
            pathDataList.Add(pathData);

            System.IO.File.WriteAllText(DataFilePath, System.Text.Json.JsonSerializer.Serialize(pathDataList));
        }

        private static List<KnightPathResponse> LoadAllPathData()
        {
            if (!System.IO.File.Exists(DataFilePath))
            {
                return new List<KnightPathResponse>();
            }

            var jsonData = System.IO.File.ReadAllText(DataFilePath);
            return System.Text.Json.JsonSerializer.Deserialize<List<KnightPathResponse>>(jsonData) ?? new List<KnightPathResponse>();
        }

        private static KnightPathResponse LoadPathData(string operationId)
        {
            var pathDataList = LoadAllPathData();
            return pathDataList.FirstOrDefault(p => p.OperationId == operationId);
        }

        private static bool IsValidPosition(string position)
        {
            if (position.Length != 2)
            {
                return false;
            }

            char file = position[0];
            char rank = position[1];

            return file >= 'A' && file <= 'H' && rank >= '1' && rank <= '8';
        }

        public class KnightPathResponse
        {
            public string Starting { get; set; }
            public string Ending { get; set; }
            public string ShortestPath { get; set; }
            public int NumberOfMoves { get; set; }
            public string OperationId { get; set; }
        }
    }
}
