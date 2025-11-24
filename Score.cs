using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisWPF
{
    // score history class
    class Score
    {
        public int Id { get; set; }
        // foreign key to User
        public int UserId { get; set; }
        // navigation property to User
        // this user doesn't show in the database, it's just for navigation in code
        public User User { get; set; }
        public int PlayedScore { get; set; }
        public DateTime ScoreDate { get; set; }
    }
}
