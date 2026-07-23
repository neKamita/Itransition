using System;
using System.ComponentModel.DataAnnotations;
using Itransition.Models.Attributes;

namespace Itransition.ViewModel
{
    public class AttributeDefinitionViewModel
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Please enter a name")]
        [MaxLength(50, ErrorMessage = "Name cannot exceed 50 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a category")]
        public Guid CategoryId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please select a data type")]
        public AttributeDataType DataType { get; set; }

        public bool IsBuiltIn { get; set; }

        public uint Version { get; set; }
    }
}
