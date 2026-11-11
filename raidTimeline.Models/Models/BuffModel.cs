namespace raidTimeline.Models.Models
{
    public class BuffModel
    {
        public string Name { get; set; } = default!;
        public string Icon { get; set; } = default!;
        public bool Stacking { get; set; }
        public double Value { get; set; }
        public double? Stacks { get; set; }
        public string Id { get; set; } = default!;
        public int Index { get; set; }
    }
}
