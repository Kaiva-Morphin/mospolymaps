using Godot;
using Godot.Collections;
using System.Linq;


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
	public Dictionary<String, HashSet<String>> graph;
	public Dictionary<(String, String), float> distances;

	public Dictionary<String, HashSet<String>> node_to_groups;
	public Dictionary<String, HashSet<String>> group_to_node;



	
	public String Prepare()
	{
		Array
		// List<String> used like (String, String) because godot cant parse Tuple to json :sad_emoji:
		// Same for HashSet<String>
		var save_graph = new Dictionary<String, List<String>>{};
		foreach (var key in graph.Keys){
			save_graph[key] = graph[key].ToList();
		}

		var save_distances = new Dictionary<List<String>, float>{};
		foreach (var key in distances.Keys){
			save_distances[new List<String>{key.Item1, key.Item2}] = distances[key];
		}

		var save_node_to_groups = new Dictionary<String, List<String>>{};
		foreach (var key in node_to_groups.Keys){
			save_node_to_groups[key] = node_to_groups[key].ToList();
		}

		var save_group_to_node = new Dictionary<String, List<String>>{};
		foreach (var key in group_to_node.Keys){
			save_group_to_node[key] = group_to_node[key].ToList();
		}

		return JSON.Print(save_graph) + "\n" + JSON.Print(save_distances) + "\n" + JSON.Print(save_node_to_groups) + "\n" + JSON.Print(save_group_to_node);
	}
	public static GraphData Parse(String data)
	{
		var parsed = data.Split("\n");
		var parsed_graph = JSON.Parse(parsed[0]).Result as Dictionary<String, List<String>>;
		var parsed_distances = JSON.Parse(parsed[1]).Result as Dictionary<List<String>, float>;
		var parsed_node_to_groups = JSON.Parse(parsed[2]).Result as Dictionary<String, List<String>>;
		var parsed_group_to_node = JSON.Parse(parsed[3]).Result as Dictionary<String, List<String>>;
		var graph_data =  new GraphData{};
		foreach (var key in parsed_graph.Keys){
			graph_data.graph[key] = new HashSet<String>(parsed_graph[key]);
		}
		foreach (var key in parsed_distances.Keys){
			graph_data.distances[(key[0], key[1])] = parsed_distances[key];
		}
		foreach (var key in parsed_node_to_groups.Keys){
			graph_data.node_to_groups[key] = new HashSet<String>(parsed_node_to_groups[key]);
		}
		foreach (var key in parsed_group_to_node.Keys){
			graph_data.group_to_node[key] = new HashSet<String>(parsed_group_to_node[key]);
		}
		
		return graph_data;
	}
}

public class Pathfinder : Node
{

	private Dictionary<String, HashSet<String>> graph;
	private Dictionary<(String, String), float> distances;
	private Dictionary<String, HashSet<String>> node_to_groups;
	private Dictionary<String, HashSet<String>> group_to_node;
	
    public override void _Ready()
    {
        graph = new Dictionary<String, HashSet<String>> {};
		distances = new Dictionary<(String, String), float> {};
		node_to_groups = new Dictionary<String, HashSet<String>> {};
		group_to_node = new Dictionary<String, HashSet<String>> {};

		var original = new Dictionary<String, List<String>> {};
		original.Add("1", new List<String>{"2"});
		var encoded = JSON.Print(original);
		var decoded = JSON.Parse(encoded).Result as Godot.Collections.Dictionary;
		GD.Print(decoded);
		using (var file = new File())
		{
			file.Open("SAVE", File.ModeFlags.Read);
			var data = file.GetAsText();
			file.Close();
			var parsed_graph = JSON.Parse(data.Split("\n")[0]).Result;
			GD.Print(parsed_graph as Dictionary<String, List<String>>);
		}
		//Load("SAVE");
    }

	public void Save(String filename){
		using (var file = new File())
		{
            var data = new GraphData{
				graph = graph,
				distances = distances,
				node_to_groups = node_to_groups,
				group_to_node = group_to_node,
			};
			file.Open(filename, File.ModeFlags.Write);
			file.StoreString(data.Prepare());
			file.Close();
		}
	}

	public void Load(String filename){
		graph = new Dictionary<String, HashSet<String>> {};
		distances = new Dictionary<(String, String), float> {};
		node_to_groups = new Dictionary<String, HashSet<String>> {};
		group_to_node = new Dictionary<String, HashSet<String>> {};
		using (var file = new File())
		{
			file.Open(filename, File.ModeFlags.Read);
			
			var data = GraphData.Parse(file.GetAsText());
			file.Close();
			/*
			graph = data.graph;
			distances = data.distances;
			node_to_groups = data.node_to_groups;
			group_to_node = data.group_to_node;*/
			
		}
	}

	public String[] Get_all_groups(){
		return group_to_node.Keys.ToArray<String>();
	}
	public void Set_group(String node, String name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			if (!node_to_groups[node].Contains(name)){
				node_to_groups[node].Add(name);
			}
		} else {
			node_to_groups[node] = new HashSet<String> {name};
		}
		if (group_to_node.ContainsKey(name)){
			if (!group_to_node[name].Contains(node)){
				group_to_node[name].Add(node);
			}
		} else {
			group_to_node[name] = new HashSet<String> {node};
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
			graph.Add(start, new HashSet<String> {end});
		}
		if (!distances.ContainsKey((start, end))){
			distances.Add((start, end), distance);
		}
		

		if (graph.ContainsKey(end)){
			if (!graph[end].Contains(start)){
				graph[end].Add(start);
			}
		} else {
			graph.Add(end, new HashSet<String> {start});
		}
		if (!distances.ContainsKey((end, start))){
			distances.Add((end, start), distance);
		}
	}

	public void Erase_point(String point){
		if (graph.ContainsKey(point)){
			foreach (var end in graph[point]){
				if (graph[end].Contains(point)){
					graph[end].Remove(point);
				}
				if (distances.ContainsKey((point, end))){
					distances.Remove((point, end));
				}
				if (distances.ContainsKey((end, point))){
					distances.Remove((end, point));
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
		if (distances.ContainsKey((start, end))){
			distances.Remove((start, end));
		}
		if (distances.ContainsKey((end, start))){
			distances.Remove((end, start));
		}
	}
	public List<String> Shortest_path(String startVertex, String endVertex) // todo: check. may be incorrect
	{
		if (!(graph.ContainsKey(startVertex) && graph.ContainsKey(endVertex))){
			return new List<String> {};
		}
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<String, String> parent = new Dictionary<String, String>();
		Dictionary<String, float> visited = new Dictionary<String, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;
		
		while (!sequence.IsEmpty())
		{
			String currentVertex = sequence.Pop();
			if (currentVertex == endVertex){
					endVertex = currentVertex;
					break;
			}
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


