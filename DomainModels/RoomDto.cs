using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels
{
    public class RoomDto
    {
        public int Id { get; set; }
        public int RoomNumber { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool IsAvailable { get; set; }
    }
}
