using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TetrisWPF
{
    class Score
    {
        public int Id { get; set; }
        public int UserId { get; set; }      // 外键
        public User User { get; set; }       // 导航属性
        public int PlayedScore { get; set; }
        public DateTime ScoreDate { get; set; }
    }
}
