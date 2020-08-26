using System;
using System.Collections.Generic;
using UnityEngine;

public class Graph<N, E>
{
    private Dictionary<N, Dictionary<N, E>> graph;

    private const string excNodeExistsStr = "Node already exists in the Graph.";
    private const string excNodeNoExistStr = "Node does not exist in the Graph.";
    private const string excFirstNodeNoExistStr = "First node does not exist in the Graph.";
    private const string excEdgeNoExistStr = "The edge from the first node to the second node does not exist in the Graph.";


    public Graph()
    {
        graph = new Dictionary<N, Dictionary<N, E>>();
    }


    public void AddNode(N node)
    {
        if (node == null) throw new ArgumentNullException("node");
        if (graph.ContainsKey(node)) throw new ArgumentException(excNodeExistsStr);

        graph.Add(node, new Dictionary<N, E>());
    }


    public void AddEdge(N node1, N node2, E edgeData)
    {
        if (node1 == null) throw new ArgumentNullException("node1");
        if (node2 == null) throw new ArgumentNullException("node2");
        if (edgeData == null) throw new ArgumentNullException("edgeData");
        if (!graph.ContainsKey(node1)) throw new NodeNotFoundException(excFirstNodeNoExistStr);

        graph[node1].Add(node2, edgeData);
    }


    public bool RemoveNode(N node)
    {
        if (node == null) throw new ArgumentNullException("node");

        if (!graph.Remove(node)) return false;

        foreach (Dictionary<N, E> edges in graph.Values)
        {
            edges.Remove(node);
        }
        return true;
    }


    public bool RemoveEdge(N node1, N node2)
    {
        if (node1 == null) throw new ArgumentNullException("node1");
        if (node2 == null) throw new ArgumentNullException("node2");
        if (!graph.ContainsKey(node1)) throw new NodeNotFoundException(excFirstNodeNoExistStr);

        return graph[node1].Remove(node2);
    }


    public bool ContainsNode(N node)
    {
        if (node == null) throw new ArgumentNullException("node");

        return graph.ContainsKey(node);
    }


    public bool ContainsEdge(N node1, N node2)
    {
        if (node1 == null) throw new ArgumentNullException("node1");
        if (node2 == null) throw new ArgumentNullException("node2");

        if (!graph.ContainsKey(node1)) return false;
        return graph[node1].ContainsKey(node2);
    }


    public Dictionary<N, Dictionary<N, E>>.KeyCollection GetNodes()
    {
        return graph.Keys;
    }


    public Dictionary<N, E> GetNeighbours(N node)
    {
        if (node == null) throw new ArgumentNullException("node");
        if (!graph.ContainsKey(node)) throw new NodeNotFoundException(excNodeNoExistStr);

        return graph[node];
    }


    public E GetEdgeData(N node1, N node2)
    {
        if (node1 == null) throw new ArgumentNullException("node1");
        if (node2 == null) throw new ArgumentNullException("node2");
        if (!graph.ContainsKey(node1)) throw new NodeNotFoundException(excFirstNodeNoExistStr);
        if (!graph[node1].ContainsKey(node2)) throw new EdgeNotFoundException(excEdgeNoExistStr);

        return graph[node1][node2];
    }


    public void UpdateEdgeData(N node1, N node2, E edgeData)
    {
        if (node1 == null) throw new ArgumentNullException("node1");
        if (node2 == null) throw new ArgumentNullException("node2");
        if (edgeData == null) throw new ArgumentNullException("edgeData");
        if (!graph.ContainsKey(node1)) throw new NodeNotFoundException(excFirstNodeNoExistStr);
        if (!graph[node1].ContainsKey(node2)) throw new EdgeNotFoundException(excEdgeNoExistStr);

        graph[node1][node2] = edgeData;
    }


    public class NodeNotFoundException : Exception
    {
        public NodeNotFoundException() { }

        public NodeNotFoundException(string message) : base(message) { }

        public NodeNotFoundException(string message, Exception inner) : base(message, inner) { }
    }


    public class EdgeNotFoundException : Exception
    {
        public EdgeNotFoundException() { }

        public EdgeNotFoundException(string message) : base(message) { }

        public EdgeNotFoundException(string message, Exception inner) : base(message, inner) { }
    }
}
