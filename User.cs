using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisWPF
{
    // user info class
    class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        // A list of scores associated with the user
        public List<Score> Scores { get; set; }
    }
}
