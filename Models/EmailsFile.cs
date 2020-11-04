using System.Collections.Generic;

namespace Models
{
    public class EmailsFile 
    {
        public string Name { get; set; }
        public IEnumerable<string> Emails { get; set; }
    } 
}