using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using MPI;

namespace MPI_BFC_dotNET
{
    [Serializable]
    class NodeTree
    {
        public string Name { get; }
        public List<NodeTree> Children { get; }
        public bool Visited { get; private set; }

        public NodeTree(string name)
        {
            Name = name;
            Children = new();
        }

        public NodeTree AddChildren(NodeTree node)
        {
            if (node != null)
                Children.Add(node);
            return this;
        }


        public IEnumerable<NodeTree> NextChildren()
        {
            foreach (var item in Children)
                yield return item;
        }

        //MinChildrens - this is the minimum number of children in Vertex
        public IEnumerable<NodeTree> DeepNextChildren(int MinChildrens = 2)
        {
            foreach (var item in Children)
                if (item.Children.Count >= MinChildrens)
                    foreach (var deepitem in item.DeepNextChildren())
                        yield return deepitem;
                else yield return item;
        }

        public NodeTree GenerateTree(int CountOfVertex)
        {
            int i = 1;
            if (Children.Count == 0)
                AddChildren(new($"{++i}"));
            AddChildren(new($"{++i}"));
            while (i < CountOfVertex)
            {
                var currentNodes = DeepNextChildren();
                List<NodeTree> Children = new();
                foreach (var item in currentNodes)
                    Children.Add(item);

                foreach (var item in Children)
                {
                    item.AddChildren(new($"{++i}"));
                    item.AddChildren(new($"{++i}"));
                }
            }
            return this;
        }

        public override string ToString()
        {
            StringBuilder sb = new();
            sb.Append($"{this.Name} \t ({Visited}) --> ");
            foreach (var node in NextChildren())
                sb.Append($"{node.Name}  \t");
            return sb.ToString();
        }

        public string ToString(bool details)
        {
            if (details)
            {
                StringBuilder sb = new();
                sb.Append($"{ToString()} \n");
                foreach (var node in NextChildren())
                    if (node.Children.Count > 0)
                        sb.Append(node.ToString(true));
                return sb.ToString();
            }
            else return ToString();
        }
        public void VisiteDeep(bool flag = true)
        {
            Visited = flag;
            foreach (var node in NextChildren())
                if (node.Children.Count > 0)
                    node.VisiteDeep(flag);
                else
                    node.Visited = flag;
        }
    }

    /*  to run:
     *          Windows cmd:        "mpiexec.exe" [-n] [mpi params] "MPI_BFC_dotNET.exe" [sizeGraph]
     *              example:        "...\Microsoft MPI\Bin\mpiexec.exe" -n 9 "...\MyMPI_lab1\MPI_BFC_dotNET\bin\Debug\net5.0\MPI_BFC_dotNET.exe" 10000
     *                  
     *          Windows terminal:   & "mpiexec.exe" [-n] [mpi params] "MPI_BFC_dotNET.exe" [sizeGraph]
     *              example:        & "...\Microsoft MPI\Bin\mpiexec.exe" -n 9 "...\MyMPI_lab1\MPI_BFC_dotNET\bin\Debug\net5.0\MPI_BFC_dotNET.exe" 10000
     * 
     *  param:
     *          [-n] count of cpu's;
     *          [sizeGraph] count of vertex (must be greater than 30);
     *          ...
     *          !!! see the documentation for other params mpi.
     */

    class Program
    {
        static void Main(string[] args) =>
            MPI.Environment.Run(ref args, comm =>
            {
                if (comm.Rank == 0)
                {
                    int CV = int.Parse(args[0]);

                    if (CV > 30 &&
                    comm.Size is 2 or 3 or 5 or 9)
                    {
                        NodeTree graph = new("main");
                        graph.GenerateTree(CV);

                        Stopwatch watch = new();
                        watch.Start();
                        switch (comm.Size)
                        {
                            case 2:
                                comm.Send(graph, 1, 0);
                                break;
                            case 3:
                                comm.Send(graph.Children[0], 1, 0);
                                comm.Send(graph.Children[1], 2, 0);
                                break;
                            case 5:
                                comm.Send(graph.Children[0].Children[0], 1, 0);
                                comm.Send(graph.Children[0].Children[1], 2, 0);
                                comm.Send(graph.Children[1].Children[0], 3, 0);
                                comm.Send(graph.Children[1].Children[1], 4, 0);
                                break;
                            case 9:
                                comm.Send(graph.Children[0].Children[0].Children[0], 1, 0);
                                comm.Send(graph.Children[0].Children[0].Children[1], 2, 0);
                                comm.Send(graph.Children[0].Children[1].Children[0], 3, 0);
                                comm.Send(graph.Children[0].Children[1].Children[1], 4, 0);
                                comm.Send(graph.Children[1].Children[0].Children[0], 5, 0);
                                comm.Send(graph.Children[1].Children[0].Children[1], 6, 0);
                                comm.Send(graph.Children[1].Children[1].Children[0], 7, 0);
                                comm.Send(graph.Children[1].Children[1].Children[1], 8, 0);
                                break;
                        }
                        foreach (var proc in Enumerable.Range(1, comm.Size - 1))
                        {
                            NodeTree node = comm.Receive<NodeTree>(proc, 0);
                            Console.WriteLine($"from {proc} : {node.ToString(true)}");
                        }

                        watch.Stop();
                        Console.WriteLine($"Final. Time (ms): {watch.Elapsed.TotalMilliseconds} ");
                    }
                    else
                    {
                        Console.WriteLine($"[ERR] Valid params: {{[-n] 2 | 3 | 5 | 9}} {{[sizeGraph] must be greater than 30}} !!!");
                        comm.Abort(0);
                    }
                }
                else
                {
                    NodeTree node = comm.Receive<NodeTree>(0, 0);
                    Console.WriteLine($"{comm.Rank} ---->>> recived");
                    node.VisiteDeep(true);
                    comm.Send(node, 0, 0);
                }
            });
    }
}