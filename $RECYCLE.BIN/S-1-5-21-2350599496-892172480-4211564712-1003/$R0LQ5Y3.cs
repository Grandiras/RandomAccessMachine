using BenchmarkDotNet.Attributes;

namespace SearchingForFun;
public sealed class Benchmarks
{
    [Benchmark]
    public void SelectionSortBenchmark()
    {
        var list = new List<int>();
        for (var i = 0; i < 50; i++) list.Add(Random.Shared.Next(0, 1000));
        ApplySelectionSort(list);
    }

    [Benchmark]
    public void InsertionSortBenchmark()
    {
        var list = new List<int>();
        for (var i = 0; i < 50; i++) list.Add(Random.Shared.Next(0, 1000));
        ApplyInsertionSort(list);
    }

    private static void ApplySelectionSort(List<int> list)
    {
        for (var i = 0; i < list.Count - 1; i++)
        {
            var minIndex = i;
            for (var j = i + 1; j < list.Count; j++) if (list[j] < list[minIndex]) minIndex = j;

            if (minIndex != i) (list[minIndex], list[i]) = (list[i], list[minIndex]);
        }
    }

    private static void ApplyInsertionSort(List<int> list)
    {
        for (var i = 1; i < list.Count; i++)
        {
            var key = list[i];

            int j;
            for (j = i - 1; j >= 0 && list[j] > key; j--) list[j + 1] = list[j];

            list[j + 1] = key;
        }
    }
}
