using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Management.Models
{
  public class Action
  {
    public string type { get; set; }
    public string address { get; set; }
    public string body { get; set; }
  }
  public class Rule
  {
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string Name { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public List<string> Machine { get; set; }

    public string Description { get; set; }
    
    [System.ComponentModel.DataAnnotations.Required]
    public string Severity { get; set; }
    public int? Frequency { get; set; }
    public int? Period { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string Field { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string ConditionOperator { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public string Value { get; set; }
    [System.ComponentModel.DataAnnotations.Required]
    public List<Action> actions { get; set; }
  }
}
