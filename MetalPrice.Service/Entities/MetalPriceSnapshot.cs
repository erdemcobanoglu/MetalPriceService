using System.ComponentModel.DataAnnotations;

namespace MetalPrice.Service.Entities
{
    public sealed class MetalPriceSnapshot
    {
        public long Id { get; set; }

        public DateTime TakenAtUtc { get; set; }

        // SQL computed column: CAST(TakenAtUtc as date)
        public DateTime TakenAtDate { get; private set; }

        [Required, MaxLength(16)]
        public string RunSlot { get; set; } = default!; // "morning" / "evening"

        [Required, MaxLength(8)]
        public string BaseCurrency { get; set; } = "USD";

        public decimal XAU { get; set; }
        public decimal XAG { get; set; }
        public decimal XPT { get; set; }
        public decimal XPD { get; set; }

        public decimal XAU_PerUsd { get; set; }
        public decimal XAG_PerUsd { get; set; }
        public decimal XPT_PerUsd { get; set; }
        public decimal XPD_PerUsd { get; set; }


        [Required, MaxLength(64)]
        public string Source { get; set; } = default!;
    }
}
