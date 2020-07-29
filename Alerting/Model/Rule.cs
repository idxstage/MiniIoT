using System;
using System.Collections.Generic;
using System.Text;

namespace Alerting.Model
{
  public class Action
  {
    public string type { get; set; }
    public string address { get; set; }
    public string body { get; set; }
  }
  public class Rule
  {
    public string Id { get; set; }
    public string Name { get; set; }
    public List<string> Machine { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }
    public int? Frequency { get; set; }
    public int? Period { get; set; }
    public string Field { get; set; }
    public string ConditionOperator { get; set; }
    public string Value { get; set; }
    public List<Action> actions { get; set; }
  }
}
