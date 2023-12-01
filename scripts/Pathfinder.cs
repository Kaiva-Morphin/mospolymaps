using Godot;
using System;
using System.Collections.Generic;


public class WeightedSequence
{
	private class Node
	{
		public String Data { get; }
		public float Weight { get; }
		public Node Next { get; set; }

		public Node(String data, float weight)
		{
			Data = data;
			Weight = weight;
			Next = null;
		}
	}

	private Node head;

	public void Add_Element(String data, float weight)
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

	public String Pop()
	{
		String result = head.Data;
		head = head.Next;
		return result;
	}

	public bool IsEmpty()
	{
		return head is null;
	}
}

public class GraphData{
	public Dictionary<String, List<String>> graph;
	public Dictionary<List<String>, float> distances; // List<String> used like (String, String) because godot cant parse Tuple to json(((

	public Dictionary<String, List<String>> node_to_groups;
	public Dictionary<String, List<String>> group_to_node;



	
	public String Prepare()
	{
		return JSON.Print(graph) + "\n" + JSON.Print(distances) + "\n" + JSON.Print(node_to_groups) + "\n" + JSON.Print(group_to_node);
	}
	public static GraphData Parse(String data)
	{
		var parsed = data.Split();
		return new GraphData{
			graph = JSON.Parse(parsed[0]).Result as Dictionary<String, List<String>>,
			distances = JSON.Parse(parsed[1]).Result as Dictionary<List<String>, float>,
			node_to_groups = JSON.Parse(parsed[2]).Result as Dictionary<String, List<String>>,
			group_to_node = JSON.Parse(parsed[3]).Result as Dictionary<String, List<String>>,
		};
	}
}

public class Pathfinder : Node
{
	// todo: change List -> HashSet (for optimization, but HashSet can't be converted to JSON unsing JSON.Print...)
	private Dictionary<String, List<String>> graph;
	private Dictionary<List<String>, float> distances;// List<String> used like (String, String) because godot cant parse Tuple to json :sad_emoji:

	private Dictionary<String, List<String>> node_to_groups;
	private Dictionary<String, List<String>> group_to_node;
	
    public override void _Ready()
    {
        graph = new Dictionary<String, List<String>> {};
		distances = new Dictionary<List<String>, float> {};
		node_to_groups = new Dictionary<String, List<String>> {};
		group_to_node = new Dictionary<String, List<String>> {};
    }

	public void Save(){
		using (var file = new File())
		{
            var data = new GraphData{
				graph = graph,
				distances = distances,
				node_to_groups = node_to_groups,
				group_to_node = group_to_node,
			};
			file.Open("SAVE.save", File.ModeFlags.Write);
			file.StoreString(data.Prepare());
			file.Close();
		}
	}

	public void Load(){
		graph = new Dictionary<String, List<String>> {};
		distances = new Dictionary<List<String>, float> {};
		node_to_groups = new Dictionary<String, List<String>> {};
		group_to_node = new Dictionary<String, List<String>> {};
		using (var file = new File())
		{
			file.Open("SAVE.save", File.ModeFlags.Read);
			
			var data = GraphData.Parse(file.ToString());
			file.Close();
			//var recovered = DataPack2.FromJson(data);
			//GD.Print(recovered);
			
			graph = data.graph;
			distances = data.distances;
			node_to_groups = data.node_to_groups;
			group_to_node = data.group_to_node;
			
		}
	}

	public void Set_group(String node, String name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			if (!node_to_groups[node].Contains(name)){
				node_to_groups[node].Add(name);
			}
		} else {
			node_to_groups[node] = new List<String> {name};
		}
		if (group_to_node.ContainsKey(name)){
			if (!group_to_node[name].Contains(node)){
				group_to_node[name].Add(node);
			}
		} else {
			group_to_node[name] = new List<String> {node};
		}
	}

	public void Clear_groups(String node){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			var groups = node_to_groups[node];
			foreach (var group in groups){
				if (group_to_node.ContainsKey(group)){
					if (group_to_node[group].Contains(node)){
						group_to_node[group].Remove(node);
					}
				}
			}
			node_to_groups.Remove(node);
		}
	}

	public void Remove_group(String node, String name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			node_to_groups[node].Remove(name);
		}
		if (group_to_node.ContainsKey(name)){
			group_to_node[name].Remove(node);
		}
	}

	public bool In_group(String node, String name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			return node_to_groups[node].Contains(name);
		}
		return false;
	}

	public void Register_bind(String start, String end, float distance){ // todo: check. may be incorrect
		if (graph.ContainsKey(start)){
			if (!graph[start].Contains(end)){
				graph[start].Add(end);
			}
		} else {
			graph.Add(start, new List<String> {end});
		}
		if (!distances.ContainsKey(new List<String>{start, end})){
			distances.Add(new List<String>{start, end}, distance);
		}
		

		if (graph.ContainsKey(end)){
			if (!graph[end].Contains(start)){
				graph[end].Add(start);
			}
		} else {
			graph.Add(end, new List<String> {start});
		}
		if (!distances.ContainsKey(new List<String>{end, start})){
			distances.Add(new List<String>{end, start}, distance);
		}
	}

	public void Erase_point(String point){
		if (graph.ContainsKey(point)){
			foreach (var end in graph[point]){
				if (graph[end].Contains(point)){
					graph[end].Remove(point);
				}
				if (distances.ContainsKey(new List<String>{point, end})){
					distances.Remove(new List<String>{point, end});
				}
				if (distances.ContainsKey(new List<String>{end, point})){
					distances.Remove(new List<String>{end, point});
				}
			}
			graph.Remove(point);
		}
	}

	public void Break_bind(String start, String end){ // todo: check. may be incorrect
		if (graph.ContainsKey(start)){
			if (graph[start].Contains(end)){
				graph[start].Remove(end);
			}
		}
		if (graph.ContainsKey(end)){
			if (graph[end].Contains(start)){
				graph[end].Remove(start);
			}
		}
		if (distances.ContainsKey(new List<String>{start, end})){
			distances.Remove(new List<String>{start, end});
		}
		if (distances.ContainsKey(new List<String>{end, start})){
			distances.Remove(new List<String>{end, start});
		}
	}
	public List<String> Shortest_path(String startVertex, String endVertex) // todo: check. may be incorrect
	{
		if (!(graph.ContainsKey(startVertex) && graph.ContainsKey(endVertex))){
			return new List<String> {};
		}
		GD.Print(startVertex);
		GD.Print(endVertex);
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<String, String> parent = new Dictionary<String, String>();
		Dictionary<String, float> visited = new Dictionary<String, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;
		GD.Print("SEQ");
		
		while (!sequence.IsEmpty())
		{
			String currentVertex = sequence.Pop();
			if (currentVertex == endVertex){
					endVertex = currentVertex;
					break;
			}
			foreach (var neighbor in graph[currentVertex])
			{
				float dist = visited[currentVertex] + distances[new List<String>{currentVertex, neighbor}];
				if (!visited.ContainsKey(neighbor))
				{
					sequence.Add_Element(neighbor, dist);
					visited[neighbor] = dist;
					parent[neighbor] = currentVertex;
				}
				else {
					if (visited[neighbor] > dist){
						visited[neighbor] = dist;
						parent[neighbor] = currentVertex;
					}
				}
			}
		}

		// Reconstruct the path
		List<String> shortestPath = new List<String>();
		String tempVertex = endVertex;
		if (!parent.ContainsKey(tempVertex)){
			return new List<String> {};
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
	
	public List<String> Shortest_path_group(String startVertex, String endGroup) // todo: check. may be incorrect
	{
		if (!graph.ContainsKey(startVertex)){
			return new List<String> {};
		}
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<String, String> parent = new Dictionary<String, String>();
		Dictionary<String, float> visited = new Dictionary<String, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;
		String endVertex = null;
		while (!sequence.IsEmpty())
		{
			String currentVertex = sequence.Pop();
			if (In_group(currentVertex, endGroup)){
					endVertex = currentVertex;
					break;
			}
			foreach (var neighbor in graph[currentVertex])
			{
				float dist = visited[currentVertex] + distances[new List<String>{currentVertex, neighbor}];
				if (!visited.ContainsKey(neighbor))
				{
					sequence.Add_Element(neighbor, dist);
					visited[neighbor] = dist;
					parent[neighbor] = currentVertex;
				}
				else {
					if (visited[neighbor] > dist){
						visited[neighbor] = dist;
						parent[neighbor] = currentVertex;
					}
				}
			}
		}
		if (endVertex == null){ // ?
			return new List<String> {};
		}
		// Reconstruct the path
		List<String> shortestPath = new List<String>();
		String tempVertex = endVertex;
		if (!parent.ContainsKey(tempVertex)){ // ?
			return new List<String> {};
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


