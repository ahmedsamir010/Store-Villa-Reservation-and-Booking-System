namespace Villa_PL.ViewModel
{
    public class RadialBarChartVM
    {
        public decimal TotalCount { get; set; }

        public decimal CountInCurrentMonth { get; set; }

        public bool HasRatioIncreased { get; set; }

        public List<int> Series { get; set; }

    }
}
