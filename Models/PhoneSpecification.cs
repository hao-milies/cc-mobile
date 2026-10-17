namespace CCMobile.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    public class PhoneSpecification
    {
         [Key]
        [ForeignKey(nameof(Product))]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập chip xử lý.")]
        [StringLength(200)]
        public string Chipset { get; set; } = string.Empty;

        [Range(1, 128)]
        public int RamGb { get; set; }

        [Range(1, 8192)]
        public int StorageGb { get; set; }

        [Range(1, 15)]
        public double ScreenSizeInches { get; set; }

        [StringLength(100)]
        public string? ScreenResolution { get; set; }

        [StringLength(100)]
        public string? PanelType { get; set; }

        [Range(1, 1000)]
        public int? RefreshRateHz { get; set; }

        [StringLength(300)]
        public string? RearCamera { get; set; }

        [StringLength(200)]
        public string? FrontCamera { get; set; }

        [Range(1, 50000)]
        public int? BatteryMah { get; set; }

        [Range(1, 500)]
        public int? ChargingWatts { get; set; }

        [StringLength(100)]
        public string? OperatingSystem { get; set; }

        [Range(1, 2000)]
        public double? WeightGrams { get; set; }

        public Product Product { get; set; } = null!;
    }
}
