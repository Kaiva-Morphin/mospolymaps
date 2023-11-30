extends Spatial

enum Mode {
	Make_Line,
	Remove_Line,
	Set_Endpoint,
	Set_Ladder,
	Clear,
	Test_Path,
	Add_Point,
	Remove_Point,
}

var current_mode = Mode.Make_Line
var line_bindings = {}
var groups = []
var groups_bindings = {}

# (S, S) -> L
var floor_lines = {}


func _ready():
	pass

var building_base
var building_roof
var building_floors = []

var node_preview_floors = {}

func parse_glb(path):
	var scene = load(path).instance()
	for c in scene.get_children():
		scene.remove_child(c)
		$Model.add_child(c)
	
	
	var mat = SpatialMaterial.new()
	mat.flags_transparent = true
	mat.albedo_color.a = 0.5
	
	
	building_base = $Model.get_node("FLOOR")
	building_roof = $Model.get_node("ROOF")
	building_floors = []
	$Control/FLOOR.clear()
	for i in range(1, 100): # todo: add support for negative floors
		var node = $Model.get_node_or_null(str(i))
		if node:
			var pathnode = Spatial.new()
			pathnode.name = str(i)
			$Paths.add_child(pathnode)
			for to_materialize in node.get_children():
				if to_materialize is MeshInstance:
					to_materialize.material_override = mat
			building_floors.append(node)
			$Control/FLOOR.add_item("Floor" + str(i), i)
		else:
			break
	
	var mesh = MeshInstance.new()
	mesh.mesh = SphereMesh.new()
	mesh.scale = Vector3(0.1, 0.1, 0.1)
	mesh.material_override = SpatialMaterial.new()
	mesh.material_override.resource_local_to_scene = true
	mesh.material_override.albedo_color = Color.red
	mesh.name = "MESH"
	var area = Area.new()
	area.name = "AREA"
	var collision = CollisionShape.new()
	collision.name = "SHAPE"
	collision.scale = Vector3(0.1, 0.1, 0.1)
	collision.shape = BoxShape.new()
	area.add_child(collision)
	
	
	for key in node_preview_floors.keys():
		for node in node_preview_floors[key]:
			node.queue_free()
	node_preview_floors = {}
	
	for key in floor_lines.keys():
		for node in floor_lines[key]:
			node.queue_free()
	floor_lines = {}
	
	for floor_node_idx in range(len(building_floors)):
		var floor_node = building_floors[floor_node_idx]
		var real_idx = floor_node_idx + 1
		var nodes = []
		for node in floor_node.get_node(str(real_idx) + "_NODES").get_children():
			nodes.append(node)
			var unique_mesh : MeshInstance = mesh.duplicate()
			unique_mesh.material_override = mesh.material_override.duplicate(true)
			var ar = area.duplicate()
			ar.add_child(unique_mesh)
			var label = MeshInstance.new()
			label.name = "LABEL"
			label.rotation.x = PI / 2
			label.translation.y = 1
			label.mesh = TextMesh.new()
			label.mesh.pixel_size = 0.008
			label.material_override = SpatialMaterial.new()
			label.material_override.albedo_color = Color.black
			label.mesh.font = DynamicFont.new()
			label.mesh.font.font_data = load("res://TerminusTTF-4.49.3.ttf")
			ar.add_child(label)
			node.add_child(ar)
		node_preview_floors[floor_node_idx] = nodes
		floor_lines[floor_node_idx] = {}
	mesh.queue_free()
	area.queue_free()
	show_floor(1)


func show_floor(i):
	if building_base: building_base.visible = false
	if building_roof: building_roof.visible = false
	for f in range(len(building_floors)):
		var real_idx = f + 1
		var node = building_floors[f]
		var path_nodes = node_preview_floors[f]
		if real_idx < i:
			for pn in path_nodes:
				pn.get_node("AREA/SHAPE").disabled = true
			node.visible = false
			node.get_node(str(real_idx)+"_CEIL").visible = false
		elif real_idx == i:
			for pn in path_nodes:
				pn.get_node("AREA/SHAPE").disabled = false
			node.visible = true
			node.get_node(str(real_idx)+"_CEIL").visible = false
		else:
			for pn in path_nodes:
				pn.get_node("AREA/SHAPE").disabled = true
			node.visible = false

var space_state : PhysicsDirectSpaceState = null
func _physics_process(_delta):
	space_state = get_world().direct_space_state

var scale_multipler : float = 1
var point : Spatial
var prev_path : Path
func _input(event):
	var ray_length = 2000
	if space_state == null: return
	
	if event is InputEventKey and event.pressed:
		match event.scancode:
			KEY_W: $Camera.translation += Vector3.FORWARD
			KEY_S: $Camera.translation += Vector3.BACK
			KEY_A: $Camera.translation += Vector3.LEFT
			KEY_D: $Camera.translation += Vector3.RIGHT
	if event is InputEventMouseButton:
		if  event.button_index == BUTTON_WHEEL_DOWN:
			$Camera.size += 0.4
		if  event.button_index == BUTTON_WHEEL_UP:
			$Camera.size -= 0.4
	if event is InputEventMouseButton and event.pressed and event.button_index == 1:
		var camera = $Camera
		var from = camera.project_ray_origin(event.position)
		var to = from + camera.project_ray_normal(event.position) * ray_length
		var result = space_state.intersect_ray(from, to, [ ], 0x7FFFFFFF, true, true)
		if result:
			var node : Spatial = result.collider
			if point == null:
				match current_mode:
					Mode.Clear:
						node.get_node("MESH").material_override.albedo_color = Color.red
						point = null
					Mode.Set_Endpoint:
						node.get_node("LABEL").mesh.text = "333333"
						point = null
					Mode.Set_Ladder:
						node.get_node("MESH").material_override.albedo_color = Color.deeppink
						point = null
					Mode.Add_Point:
						pass
					Mode.Remove_Point:
						PATHFINDER.call("Erase_point", node.get_parent())
						node.queue_free()
					_:
						node.get_node("MESH").scale = Vector3(0.15, 0.15, 0.15)
						point = node
					
			else:
				if point != node:
					match current_mode:
						Mode.Make_Line:
							var distance = node.get_parent().translation.distance_to(point.get_parent().translation)
							point.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
							node.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
							point.get_node("MESH").material_override.albedo_color = Color.yellow
							node.get_node("MESH").material_override.albedo_color = Color.yellow
							PATHFINDER.call("Register_bind", node.get_parent(), point.get_parent(), distance)
							add_line(node.get_parent(), point.get_parent())
							point = node
							point.get_node("MESH").scale = Vector3(0.15, 0.15, 0.15)
						Mode.Test_Path:
							point.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
							node.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
							if prev_path:
								prev_path.queue_free()
							var res = PATHFINDER.call("Shortest_path", node.get_parent(), point.get_parent())
							point = null
							var line = Path.new()
							line.set_script(line_script)
							line.global_coords = true
							line.default_color = Color.green
							line.resolution = 10
							line.width = 0.04
							line.curve = Curve3D.new()
							for p in res: 
								line.curve.add_point(p.translation)
							add_child(line)
							prev_path = line
						Mode.Remove_Line:
							point.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
							node.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
							var res = PATHFINDER.call("Break_bind", node.get_parent(), point.get_parent())
							remove_line(node.get_parent(), point.get_parent())
							point = null
		else:
			if point:
				point.get_node("MESH").scale = Vector3(0.1, 0.1, 0.1)
				point = null





var line_script = load("res://Path.gd")
func add_line(node, point):
	var line = Path.new()
	line.set_script(line_script)
	line.global_coords = true
	line.width = 0.02
	line.resolution = 100
	line.curve = Curve3D.new()
	line.curve.add_point(node.global_translation)
	line.curve.add_point(point.global_translation)
	var i = $Control/FLOOR.selected
	node.add_child(line)
	floor_lines[i][[node, point]] = line;

func remove_line(node, point):
	
	var i = $Control/FLOOR.selected
	if floor_lines[i].has([node, point]):
		floor_lines[i][[node, point]].queue_free()
		floor_lines[i].erase([node, point])
	elif floor_lines[i].has([point, node]):
		floor_lines[i][[point, node]].queue_free()
		floor_lines[i].erase([point, node])
	else:
		pass
	

func _on_OptionButton_item_selected(index):
	match index:
		0:current_mode = Mode.Make_Line
		1: current_mode = Mode.Remove_Line
		2: current_mode = Mode.Set_Endpoint
		3: current_mode = Mode.Set_Ladder
		4: current_mode = Mode.Clear
		5: current_mode = Mode.Test_Path
		6: current_mode = Mode.Add_Point
		7: current_mode = Mode.Remove_Point
	point = null





func _on_Load_pressed():
	for c in $Model.get_children():
		c.queue_free()
	node_preview_floors = {}
	call_deferred("parse_glb", "res://result.glb")

	#$Control/FileDialog.popup_centered()


func _on_FileDialog_file_selected(path):
	for c in $Model.get_children():
		c.queue_free()
	parse_glb(path)


func _on_NextFloor_pressed():
	pass


func _on_FLOOR_item_selected(index):
	show_floor(index + 1)


func _on_LineEdit_text_entered(new_text):
	scale_multipler = float(new_text)
	for i in node_preview_floors.keys():
		for node in node_preview_floors[i]:
			node.scale = Vector3.ONE * scale_multipler 

func _on_Save_pressed():
	PATHFINDER.call("Save", "data")


func _on_LoadTscn_pressed():
	PATHFINDER.call("Load", "data")
