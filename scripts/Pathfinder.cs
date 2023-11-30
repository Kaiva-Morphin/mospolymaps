using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;


public class WeightedSequence
{
	private class Node
	{
		public Spatial Data { get; }
		public float Weight { get; }
		public Node Next { get; set; }

		public Node(Spatial data, float weight)
		{
			Data = data;
			Weight = weight;
			Next = null;
		}
	}

	private Node head;

	public void Add_Element(Spatial data, float weight)
	{
		Node newNode = new Node(data, weight);

		if (head == null || head.Weight > weight)
		{
			newNode.Next = head;
			head = newNode;
		}
		else
		{
			Node current = head;

			while (current.Next != null && current.Next.Weight <= weight)
			{
				current = current.Next;
			}

			newNode.Next = current.Next;
			current.Next = newNode;
		}
	}

	public Spatial Pop()
	{
		Spatial result = head.Data;
		head = head.Next;
		return result;
	}

	public bool IsEmpty()
	{
		return head is null;
	}
}


public class DataPack : Godot.Object{
	public Dictionary<Spatial, HashSet<Spatial>> graph;
	public Dictionary<(Spatial, Spatial), float> distances;

	public Dictionary<Spatial, HashSet<string>> node_to_groups;
	public Dictionary<string, HashSet<Spatial>> group_no_node;
	public string ToJson()
	{
		return JSON.Print(this);
	}
	public static DataPack FromJson(string json)
	{
		return JSON.Parse(json).Result as DataPack;
	}
}

public class Pathfinder : Node
{
	
	private Dictionary<Spatial, HashSet<Spatial>> graph;
	private Dictionary<(Spatial, Spatial), float> distances;

	private Dictionary<Spatial, HashSet<string>> node_to_groups;
	private Dictionary<string, HashSet<Spatial>> group_no_node;

	public void Save(string filename){
		using (var file = new File())
		{
			file.Open(filename, File.ModeFlags.Write);
			var data = new DataPack
			{
				graph = graph,
				distances = distances,
				node_to_groups = node_to_groups,
				group_no_node = group_no_node
			};
			file.StoreVar(data);
			file.Close();
		}
	}
	public void Load(string filename){
		graph.Clear();
		distances.Clear();
		node_to_groups.Clear();
		group_no_node.Clear();
		using (var file = new File())
		{
			file.Open(filename, File.ModeFlags.Read);
			
			var data = (DataPack)file.GetVar();
			file.Close();
			GD.Print(file.GetVar());
			
			/*graph = data.graph;
			distances = data.distances;
			node_to_groups = data.node_to_groups;
			group_no_node = data.group_no_node;*/
			
		}
	}
	public override void _Ready()
	{
		graph = new Dictionary<Spatial, HashSet<Spatial>> {};
		distances = new Dictionary<(Spatial, Spatial), float> {};
		node_to_groups = new Dictionary<Spatial, HashSet<string>> {};
		group_no_node = new Dictionary<string, HashSet<Spatial>> {};
	}
	
	public void Set_group(Spatial node, string name){
		if (node_to_groups.Keys.Contains(node)){
			node_to_groups[node].Add(name);
		} else {
			node_to_groups[node] = new HashSet<string> {name};
		}
		if (group_no_node.Keys.Contains(name)){
			group_no_node[name].Add(node);
		} else {
			group_no_node[name] = new HashSet<Spatial> {node};
		}
	}

	public void Clear_groups(Spatial node){
		if (node_to_groups.Keys.Contains(node)){
			var groups = node_to_groups[node];
			foreach (var group in groups){
				if (group_no_node.Keys.Contains(group)){
					if (group_no_node[group].Contains(node)){
						group_no_node[group].Remove(node);
					}
				}
			}
			node_to_groups.Remove(node);
		}
	}

	public void Remove_group(Spatial node, string name){
		if (node_to_groups.Keys.Contains(node)){
			node_to_groups[node].Remove(name);
		}
		if (group_no_node.Keys.Contains(name)){
			group_no_node[name].Remove(node);
		}
	}

	public bool In_group(Spatial node, string name){
		if (node_to_groups.Keys.Contains(node)){
			return node_to_groups[node].Contains(name);
		}
		return false;
	}

	public void Register_bind(Spatial start, Spatial end, float distance){
		if (graph.Keys.Contains(start)){
			if (!graph[start].Contains(end)){
				graph[start].Add(end);
			}
		} else {
			graph.Add(start, new HashSet<Spatial> {end});
		}
		if (!distances.Keys.Contains((start, end))){
			distances.Add((start, end), distance);
		}
		

		if (graph.Keys.Contains(end)){
			if (!graph[end].Contains(start)){
				graph[end].Add(start);
			}
		} else {
			graph.Add(end, new HashSet<Spatial> {start});
		}
		if (!distances.Keys.Contains((end, start))){
			distances.Add((end, start), distance);
		}
	}

	public void Erase_point(Spatial point){
		if (graph.Keys.Contains(point)){
			foreach (var end in graph[point]){
				if (graph[end].Contains(point)){
					graph[end].Remove(point);
				}
				if (distances.Keys.Contains((point, end))){
					distances.Remove((point, end));
				}
				if (distances.Keys.Contains((end, point))){
					distances.Remove((end, point));
				}
			}
			graph.Remove(point);
		}
	}

	public void Break_bind(Spatial start, Spatial end){
		if (graph.Keys.Contains(start)){
			if (graph[start].Contains(end)){
				graph[start].Remove(end);
			}
		}
		if (graph.Keys.Contains(end)){
			if (graph[end].Contains(start)){
				graph[end].Remove(start);
			}
		}
		if (distances.Keys.Contains((start, end))){
			distances.Remove((start, end));
		}
		if (distances.Keys.Contains((end, start))){
			distances.Remove((end, start));
		}
	}
	public List<Spatial> Shortest_path(Spatial startVertex, Spatial endVertex)
	{
		if (!(graph.Keys.Contains(startVertex) && graph.Keys.Contains(endVertex))){
			return new List<Spatial> {};
		}
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<Spatial, Spatial> parent = new Dictionary<Spatial, Spatial>();
		Dictionary<Spatial, float> visited = new Dictionary<Spatial, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;

		while (!sequence.IsEmpty())
		{
			Spatial currentVertex = sequence.Pop();
			
			foreach (var neighbor in graph[currentVertex])
			{
				float dist = visited[currentVertex] + distances[(currentVertex, neighbor)];
				if (!visited.ContainsKey(neighbor))
				{
					sequence.Add_Element(neighbor, dist);
					visited[neighbor] = dist;
					parent[neighbor] = currentVertex;
				}
				/*else {
					if (visited[neighbor] > dist){
						visited[neighbor] = dist;
					}
				}*/
			}
		}

		// Reconstruct the path
		List<Spatial> shortestPath = new List<Spatial>();
		Spatial tempVertex = endVertex;
		if (!parent.Keys.Contains(tempVertex)){
			return new List<Spatial> {};
		}
		while (tempVertex != startVertex)
		{
			shortestPath.Add(tempVertex);
			tempVertex = parent[tempVertex];
		}

		shortestPath.Add(startVertex);
		shortestPath.Reverse();

		return shortestPath;
	}
	
	public List<Spatial> Shortest_path_group(Spatial startVertex, string endGroup)
	{
		if (!graph.Keys.Contains(startVertex)){
			return new List<Spatial> {};
		}
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<Spatial, Spatial> parent = new Dictionary<Spatial, Spatial>();
		Dictionary<Spatial, float> visited = new Dictionary<Spatial, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;
		Spatial endVertex = null;
		while (!sequence.IsEmpty())
		{
			Spatial currentVertex = sequence.Pop();
			foreach (var neighbor in graph[currentVertex])
			{
				float dist = visited[currentVertex] + distances[(currentVertex, neighbor)];
				if (!visited.ContainsKey(neighbor))
				{
					sequence.Add_Element(neighbor, dist);
					visited[neighbor] = dist;
					parent[neighbor] = currentVertex;
				}
				else {
					if (visited[neighbor] > dist){
						visited[neighbor] = dist;
					}
				}
				if (In_group(neighbor, endGroup)){
					endVertex = neighbor;
					break;
				}
			}
		}
		if (endVertex == null){
			return new List<Spatial> {};
		}
		// Reconstruct the path
		List<Spatial> shortestPath = new List<Spatial>();
		Spatial tempVertex = endVertex;
		if (!parent.Keys.Contains(tempVertex)){
			return new List<Spatial> {};
		}
		while (tempVertex != startVertex)
		{
			shortestPath.Add(tempVertex);
			tempVertex = parent[tempVertex];
		}

		shortestPath.Add(startVertex);
		shortestPath.Reverse();

		return shortestPath;
	}
}
