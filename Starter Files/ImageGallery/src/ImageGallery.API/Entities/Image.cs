using System;
using System.ComponentModel.DataAnnotations;

namespace ImageGallery.API.Entities
{
    /// <summary>
    /// QQHQ :: This is only for DB ops. No serialization over the wire.
    /// Client should use different entity for serializing the result data.
    /// </summary>
    public class Image
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        [Required]
        [MaxLength(200)]
        public string FileName { get; set; }

        [Required]
        [MaxLength(50)]
        public string OwnerId { get; set; }
    }
}
