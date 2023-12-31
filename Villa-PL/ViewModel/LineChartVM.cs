namespace Villa_PL.ViewModel
{
    public class LineChartVM
    {
        public List<ChartData> Series  { get; set; }
        public string[] Categories { get; set; }

    }
    public class ChartData
    {
        public string Name { get; set; }

        public int[] data { get; set; }
    }


}
