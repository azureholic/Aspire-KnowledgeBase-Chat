using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KBChat.Api.Models;


public class WikiPages
{
    public string path { get; set; }
    public int order { get; set; }
    public bool isParentPage { get; set; }
    public string gitItemPath { get; set; }
    public List<Subpage> subPages { get; set; }
    public string url { get; set; }
    public string remoteUrl { get; set; }
    public string content { get; set; }
}

public class Subpage
{
    public string path { get; set; }
    public int order { get; set; }
    public string gitItemPath { get; set; }
    public object[] subPages { get; set; }
    public string url { get; set; }
    public string remoteUrl { get; set; }
}



