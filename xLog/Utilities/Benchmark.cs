using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// A helpful class for performing benchmarks to get the average execution time of a task.
/// </summary>
public class Benchmark
{
    /// <summary>
    /// Average execution time in seconds.
    /// </summary>
    public double Avg { get { return sw.Elapsed.TotalSeconds / (double)Cycles; } }
    /// <summary>
    /// Average execution time in milliseconds.
    /// </summary>
    public double AvgMs { get { return sw.ElapsedMilliseconds / (double)Cycles; } }
    public int Cycles { get; private set; }
    private Stopwatch sw = new Stopwatch();
    private string Name = null;


    public Benchmark(string name = null) { Cycles = 0; Name = name; }

    public void Start()
    {
        Cycles++;
        sw.Start();
    }

    public void Stop()
    {
        sw.Stop();
    }


    public override string ToString()
    {
        return string.Concat((Name!=null? ("\""+Name+"\"") : string.Empty), " Avg ", Avg.ToString("F2"), "s <", Cycles, " Cycles>");
    }
}
