namespace pancake.lib.tests
{
    public class DifferTests
    {
        public record Row(string id)
        {
            public static IList<Row> FromString(string s)
                => s.Select(r => new Row(r.ToString())).ToList();

            public static string ToString(IList<Row> rows)
                => string.Join("", rows.Select(r => r.id));
        }

        private static string DiffAndApply(string oldText, string newText)
        {
            var oldList = Row.FromString(oldText);
            var newList = Row.FromString(newText);

            Differ<Row>.Instance.ApplyDiffsToOld(oldList, newList, r => r.id);

            return Row.ToString(oldList);
        }

        [Fact]
        public void Add_To_End()
        {
            var oldText = "abcd";
            var newText = "abcde";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }

        [Fact]
        public void Add_Inserts()
        {
            var oldText = "";
            var newText = "abcd";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }

        [Fact]
        public void Add_Deletes()
        {
            var oldText = "abcd";
            var newText = "";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }


        [Fact]
        public void Add_To_Beginning()
        {
            var oldText = "-abcd";
            var newText = "abcd";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }

        [Fact]
        public void Delete_From_Beginning()
        {
            var oldText = "abcd";
            var newText = "bcd";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }

        [Fact]
        public void Change_In_Middle()
        {
            var oldText = "abcd";
            var newText = "a12d";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }

        [Fact]
        public void Lots_Of_Changes()
        {
            var oldText = "ab-cd-";
            var newText = "**a12d";

            var changedText = DiffAndApply(oldText, newText);

            Assert.Equal(newText, changedText);
        }

    }
}