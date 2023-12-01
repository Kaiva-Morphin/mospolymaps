using Godot;
using Godot.Collections;
using System.Linq;

public class WeightedSequence
{
	private class Node
	{
		public string Data { get; }
		public float Weight { get; }
		public Node Next { get; set; }

		public Node(string data, float weight)
		{
			Data = data;
			Weight = weight;
			Next = null;
		}
	}

	private Node head;

	public void Add_Element(string data, float weight)
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

	public string Pop()
	{
		string result = head.Data;
		head = head.Next;
		return result;
	}

	public bool IsEmpty()
	{
		return head is null;
	}
}

public class GraphData{
	public Dictionary<string, Array<string>> graph;
	public Dictionary<(string, string), float> distances;

	public Dictionary<string, Array<string>> node_to_groups;
	public Dictionary<string, Array<string>> group_to_node;



	
	public string Prepare()
	{
		// Array<string> used like (string, string) because godot cant parse Tuple to json :sad_emoji:
		// Same for Array<string>
		var save_graph = new Dictionary<string, Array<string>>{};
		foreach (var key in graph.Keys){
			save_graph[key] = new Array<string>{};
			foreach (var val in graph[key]){
				save_graph[key].Append(val);
			}
		}

		var save_distances = new Dictionary<Array<string>, float>{};
		foreach (var key in distances.Keys){
			save_distances[new Array<string>{key.Item1, key.Item2}] = distances[key];
		}

		var save_node_to_groups = new Dictionary<string, Array<string>>{};
		foreach (var key in node_to_groups.Keys){
			save_node_to_groups[key] = new Array<string>{};
			foreach (var val in node_to_groups[key]){
				save_graph[key].Append(val);
			}
		}

		var save_group_to_node = new Dictionary<string, Array<string>>{};
		foreach (var key in group_to_node.Keys){
			save_group_to_node[key] = new Array<string>{};
			foreach (var val in group_to_node[key]){
				save_graph[key].Append(val);
			}
		}

		return JSON.Print(save_graph) + "\n" + JSON.Print(save_distances) + "\n" + JSON.Print(save_node_to_groups) + "\n" + JSON.Print(save_group_to_node);
	}
	public static GraphData Parse(string data)
	{
		var graph_data =  new GraphData{
			graph = new Dictionary<string, Array<string>>{},
			distances = new Dictionary<(string, string), float>{},
			node_to_groups = new Dictionary<string, Array<string>>{},
			group_to_node = new Dictionary<string, Array<string>> {}
		};
		var parsed = data.Split("\n");
		var parsed_graph = JSON.Parse(parsed[0]).Result as Dictionary;
		// pizdec
		foreach (string key in parsed_graph.Keys){
			var arr = parsed_graph[key] as Array;
			var hashset = new Array<string>{};
			foreach (string val in arr){
				hashset.Add(val);
			}
			graph_data.graph[key] = hashset;
		}

		var parsed_distances = JSON.Parse(parsed[1]).Result as Dictionary;
		foreach (var key in parsed_distances.Keys){
			GD.Print(key);
			var keyd = key as Array;
			//var a = keyd[0] as string;
			//var b = keyd[1] as string;
			//var val = parsed_distances[key];
			//GD.Print(val);
			//graph_data.distances[(key[0], key[1])] = parsed_distances[key];
		}
		//var parsed_node_to_groups = JSON.Parse(parsed[2]).Result as Dictionary<string, Array<string>>;
		//var parsed_group_to_node = JSON.Parse(parsed[3]).Result as Dictionary<string, Array<string>>;
		//
		//
		
		//foreach (var key in parsed_node_to_groups.Keys){
		//	graph_data.node_to_groups[key] = new Array<string>(parsed_node_to_groups[key]);
		//}
		//foreach (var key in parsed_group_to_node.Keys){
		//	graph_data.group_to_node[key] = new Array<string>(parsed_group_to_node[key]);
		//}
		
		return graph_data;
	}
}

public class Pathfinder : Node
{

	private Dictionary<string, Array<string>> graph;
	private System.Collections.Generic.Dictionary<(string, string), float> distances; // я просто хочу быть счастливым
	private Dictionary<string, Array<string>> node_to_groups;
	private Dictionary<string, Array<string>> group_to_node;
	
	
	public override void _Ready()
	{
		graph = new Dictionary<string, Array<string>> {};
		distances = new System.Collections.Generic.Dictionary<(string, string), float> {};
		node_to_groups = new Dictionary<string, Array<string>> {};
		group_to_node = new Dictionary<string, Array<string>> {};
	}

	public void Save(string filename){
		// convert to godot's Array's
		var data = new Array{};
		
		data.Add(graph);
		var converted_distances = new Dictionary{};
		foreach(var key in distances.Keys){
			var a = key.Item1;
			var b = key.Item2;
			converted_distances.Add(new Array<string>{a, b}, distances[key]);
		}
		data.Add(converted_distances);
		data.Add(node_to_groups);
		data.Add(group_to_node);
		
		File file = new File();
        file.Open(filename, File.ModeFlags.Write);
        file.StoreVar(data);
        file.Close();
	}

	public void Load(string filename){
		File f = new File();
        f.Open(filename, File.ModeFlags.Read);
        Array decoded = f.GetVar() as Array;
        f.Close();
		graph = new Dictionary<string, Array<string>>{};
		var a_graph = decoded[0] as Dictionary;
		foreach(string key in a_graph.Keys){
			var arr = new Array<string>{};
			foreach (string val in a_graph[key] as Array){
				arr.Add(val);
			}
			graph[key] = arr;
		}
		
		distances = new System.Collections.Generic.Dictionary<(string, string), float> {};
		var a_dist = decoded[1] as Dictionary;
		foreach(Array key in a_dist.Keys){
			var val = (float)a_dist[key];
			distances[(key[0] as string, key[1] as string)] = val;
		}
		
		node_to_groups = new Dictionary<string, Array<string>> {};
		var a_ntd = decoded[2] as Dictionary;
		foreach(string key in a_ntd.Keys){
			var arr = new Array<string>{};
			foreach (string val in a_ntd[key] as Array){
				arr.Add(val);
			}
			node_to_groups[key] = arr;
		}

		group_to_node = new Dictionary<string, Array<string>> {};
		var a_tnd = decoded[3] as Dictionary;
		foreach(string key in a_tnd.Keys){
			var arr = new Array<string>{};
			foreach (string val in a_tnd[key] as Array){
				arr.Add(val);
			}
			group_to_node[key] = arr;
		}
	}

	public string[] Get_all_groups(){
		return group_to_node.Keys.ToArray<string>();
	}
	public void Set_group(string node, string name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			if (!node_to_groups[node].Contains(name)){
				node_to_groups[node].Add(name);
			}
		} else {
			node_to_groups[node] = new Array<string> {name};
		}
		if (group_to_node.ContainsKey(name)){
			if (!group_to_node[name].Contains(node)){
				group_to_node[name].Add(node);
			}
		} else {
			group_to_node[name] = new Array<string> {node};
		}
	}

	public void Clear_groups(string node){ // todo: check. may be incorrect
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

	public void Remove_group(string node, string name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			node_to_groups[node].Remove(name);
		}
		if (group_to_node.ContainsKey(name)){
			group_to_node[name].Remove(node);
		}
	}

	public bool In_group(string node, string name){ // todo: check. may be incorrect
		if (node_to_groups.ContainsKey(node)){
			return node_to_groups[node].Contains(name);
		}
		return false;
	}

	public void Register_bind(string start, string end, float distance){ // todo: check. may be incorrect
		if (graph.ContainsKey(start)){
			if (!graph[start].Contains(end)){
				graph[start].Add(end);
			}
		} else {
			graph.Add(start, new Array<string> {end});
		}
		if (!distances.ContainsKey((start, end))){
			distances.Add((start, end), distance);
		}
		

		if (graph.ContainsKey(end)){
			if (!graph[end].Contains(start)){
				graph[end].Add(start);
			}
		} else {
			graph.Add(end, new Array<string> {start});
		}
		if (!distances.ContainsKey((end, start))){
			distances.Add((end, start), distance);
		}
	}

	public void Erase_point(string point){
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

	public void Break_bind(string start, string end){ // todo: check. may be incorrect
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
	public Array<string> Shortest_path(string startVertex, string endVertex) // todo: check. may be incorrect
	{
		if (!(graph.ContainsKey(startVertex) && graph.ContainsKey(endVertex))){
			return new Array<string> {};
		}
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<string, string> parent = new Dictionary<string, string>();
		Dictionary<string, float> visited = new Dictionary<string, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;
		
		while (!sequence.IsEmpty())
		{
			string currentVertex = sequence.Pop();
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
		Array<string> shortestPath = new Array<string>();
		string tempVertex = endVertex;
		if (!parent.ContainsKey(tempVertex)){
			return new Array<string> {};
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
	
	public Array<string> Shortest_path_group(string startVertex, string endGroup) // todo: check. may be incorrect
	{
		if (!graph.ContainsKey(startVertex)){
			return new Array<string> {};
		}
		WeightedSequence sequence = new WeightedSequence();
		Dictionary<string, string> parent = new Dictionary<string, string>();
		Dictionary<string, float> visited = new Dictionary<string, float>();

		sequence.Add_Element(startVertex, 0);
		visited[startVertex] = 0;
		string endVertex = null;
		while (!sequence.IsEmpty())
		{
			string currentVertex = sequence.Pop();
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
			return new Array<string> {};
		}
		// Reconstruct the path
		Array<string> shortestPath = new Array<string>();
		string tempVertex = endVertex;
		if (!parent.ContainsKey(tempVertex)){ // ?
			return new Array<string> {};
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


