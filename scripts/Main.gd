extends Spatial


var groups = []
var ladder_groups = []
var labels = {}
func _ready():
	var font = $UI/SearchBar/Panel/PREVIEW.get_font("font")
	font.size = get_viewport().size.x / 600 * 24
	$UI/SearchBar.rect_position.y = $UI.rect_size.y * 0.95
	parse_glb("res://resources/models/result.glb")
	PATHFINDER.call("Load", "res://MapData.save")
	labels = {}
	groups = []
	ladder_groups = []
	var all_groups = PATHFINDER.call("Get_all_groups")
	for group in all_groups:
		if !"LADDER" in group:
			groups.append(group)
			var members = PATHFINDER.call("Get_group_members", group)
			for member in members:
				var label = get_node(member).get_node_or_null("LABEL")
				var label_floor = int(get_node(member).get_parent().get_parent().name)
				if !label_floor in labels.keys(): labels[label_floor] = []
				
				if label:
					label.text += "; " + group
				else:
					label = Label3D.new()
					label.no_depth_test = true
					label.render_priority = 1
					label.text = group
					label.name = "LABEL"
					label.billboard = true
					labels[label_floor].append(label)
					get_node(member).add_child(label)
				
		else:
			ladder_groups.append(group)
	for l in ladder_groups:
		var holders = PATHFINDER.call("Get_group_members", l)
		if len(holders) < 2: continue
		var a = get_node(holders[0]) 
		var b = get_node(holders[1])
		var dist = a.translation.distance_to(b.translation)
		PATHFINDER.call("Register_bind", holders[0], holders[1], dist)
	var font2 = $UI/FloorSlider/CurrentFloor.get_font("font")
	font2.size = get_viewport().size.x / 600 * 24 * 3
	$UI/FloorSlider/CurrentFloor.rect_pivot_offset.y = $UI/FloorSlider/CurrentFloor.rect_size.y / 2
	$UI/FloorSlider/CurrentFloor.rect_position.y = $UI/FloorSlider/Center.rect_position.y;
	var v = $UI/FloorSlider.rect_size.y * 0.35
	$UI/FloorSlider/CurrentFloor/FloorBelow.rect_position.y = v
	$UI/FloorSlider/CurrentFloor/FloorAbove.rect_position.y = -v
	
	
var building_floors = []
func parse_glb(path): # todo: autopath using raycasts
	var scene = load(path).instance()
	for c in scene.get_children():
		scene.remove_child(c)
		$Model.add_child(c)
	
	var mat = SpatialMaterial.new()
	mat.flags_transparent = true
	mat.albedo_color.a = 0.5
	
	$Model.get_node("FLOOR").show()
	$Model.get_node("FLOOR").material_override = mat.duplicate()
	$Model.get_node("ROOF").hide()
	building_floors = []
	for i in range(1, 100): # todo: add support for negative floors
		var node = $Model.get_node_or_null(str(i))
		if node:
			var pathnode = Spatial.new()
			pathnode.name = str(i)
			$Paths.add_child(pathnode)
			for to_materialize in node.get_children():
				if to_materialize is MeshInstance:
					to_materialize.material_override = mat.duplicate()
			building_floors.append(node)
		else:
			break
	
	$CameraOrigin.set_floor(1)



var last_relative = Vector2.ZERO
onready var target_pos = $UI.rect_size.y * 0.95
var search_drag = false
func _on_SearchBar_gui_input(event):
	if event is InputEventScreenDrag: # todo: add multitouch support
		$UI/SearchBar.rect_position.y = clamp($UI/SearchBar.rect_position.y + event.relative.y, 0, $UI.rect_size.y * 0.95)
		last_relative = event.relative
		
	if event is InputEventScreenTouch:
		if event.pressed:
			search_drag = true
		else:
			search_drag = false
			if abs(last_relative.y) > 5:
				if last_relative.y > 0:
					close_searchbar()
				else:
					open_searchbar()
			else:
				if event.position.y > $UI.rect_size.y * 0.5:
					close_searchbar()
				else:
					open_searchbar()

func close_searchbar():
	target_pos = $UI.rect_size.y * 0.95
	$UI/SearchBar/Panel/Path.grab_focus()

func open_searchbar():
	target_pos = 0
	$UI/SearchBar/Panel/From.grab_focus()

func search_in_groups(s):
	update_search_colors()
	groups.sort()
	var results = []
	if s == "":
		for group in groups:
			group = str(group)
			results.append(group)
	else:
		for group in groups:
			group = str(group)
			if group.to_lower().find(s.to_lower(), 0) != -1:
				results.append(group)
	return results

func update_search_colors():
	if selected[0]:
		$UI/SearchBar/Panel/From.set("custom_colors/font_color", Color.green)
	else:
		$UI/SearchBar/Panel/From.set("custom_colors/font_color", Color.white)
	if selected[1]:
		$UI/SearchBar/Panel/To.set("custom_colors/font_color", Color.green)
	else:
		$UI/SearchBar/Panel/To.set("custom_colors/font_color", Color.white)

func clear_search_menu():
	update_search_colors()
	for c in $UI/SearchBar/Panel/ScrollContainer/VBoxContainer.get_children():
		c.disconnect("gui_input", self, "select")
		c.queue_free()
	$UI/SearchBar/Panel/PREVIEW.show()

var from_selected_group = null
var to_selected_group = null
func _on_To_text_changed(new_text):
	selected[1] = false
	var res = search_in_groups(new_text)
	update_Scrolbar(res, false)

func _on_To_text_entered(new_text):
	selected[1] = false
	var res = search_in_groups(new_text)
	if len(res) == 1:
		selected[1] = true
	update_Scrolbar(res, true)

func _on_From_text_changed(new_text):
	selected[0] = false
	var res = search_in_groups(new_text)
	update_Scrolbar(res, true)

func _on_From_text_entered(new_text):
	selected[0] = false
	var res = search_in_groups(new_text)
	if len(res) == 1:
		selected[0] = true
	update_Scrolbar(res, false)

func update_Scrolbar(res, start_or_end):
	clear_search_menu()
	var button = Button.new();
	button.size_flags_horizontal = button.SIZE_EXPAND_FILL
	button.align = button.ALIGN_CENTER
	if len(res) == 0:
		$UI/SearchBar/Panel/PREVIEW.show()
	else:
		$UI/SearchBar/Panel/PREVIEW.hide()
	for el in res:
		#var b = button.duplicate()
		#b.text = el
		
		#$UI/SearchBar/Panel/ScrollContainer/VBoxContainer.add_child(b)
		var l = Label.new()
		l.size_flags_horizontal = l.SIZE_EXPAND_FILL
		l.align = l.ALIGN_CENTER
		l.valign = l.VALIGN_CENTER
		l.mouse_filter = Control.MOUSE_FILTER_PASS
		l.text = el
		l.connect("gui_input", self, "select", [el, start_or_end])
		$UI/SearchBar/Panel/ScrollContainer/VBoxContainer.add_child(l)

var last_select_relative = Vector2.ZERO
var start_pos = Vector2.ZERO
var selected = [false, false]
func select(event, s, start_or_end):
	if event is InputEventScreenDrag:
		last_select_relative = event.relative
		$UI/SearchBar/Panel/Path.grab_focus() # todo: fix! затычка
	if event is InputEventScreenTouch:
		if !event.pressed:
			if last_select_relative.length_squared() < 25 and event.position.distance_squared_to(start_pos) < 25:
				if start_or_end:
					$UI/SearchBar/Panel/From.text = s
					$UI/SearchBar/Panel/Path.grab_focus()
					selected[0] = true
				else:
					$UI/SearchBar/Panel/To.text = s
					$UI/SearchBar/Panel/Path.grab_focus()
					selected[1] = true
				if selected[0] and selected[1]:
					$UI/SearchBar/Panel/Path.show()
				else:
					$UI/SearchBar/Panel/Path.hide()
				clear_search_menu()
			last_select_relative = Vector2.ZERO
		else:
			start_pos = event.position
var line_script = load("res://scripts/Path.gd")
var prev_line = []
func _on_Path_pressed():
	var start_group = $UI/SearchBar/Panel/From.text
	var end_group = $UI/SearchBar/Panel/To.text
	var start = PATHFINDER.call("Get_group_members", start_group)[0]
	#print(start)
	var path = PATHFINDER.call("Shortest_path_group", start, end_group)
	#print(path)
	
	for l in prev_line:
		l.queue_free()
	prev_line =[]
	if path == []:
		pass
	else:
		for p in range(1, len(path)):
			var c = MeshInstance.new()
			c.mesh = SphereMesh.new()
			c.scale = Vector3(0.05, 0.05, 0.05)
			c.material_override = SpatialMaterial.new()
			c.material_override.albedo_color = Color.green
			var line = Path.new()
			line.set_script(line_script)
			line.global_coords = true
			line.default_color = Color.green
			line.resolution = 10
			line.width = 0.04
			line.curve = Curve3D.new()
			line.curve.add_point(get_node(path[p]).global_translation)
			line.curve.add_point(get_node(path[p-1]).global_translation)
			line.add_child(c)
			prev_line.append(line)
			get_node(path[p-1]).add_child(line)
	close_searchbar()
#6_LADDER_LEFT(7)
func _notification(what: int) -> void:
	match what:
		MainLoop.NOTIFICATION_WM_GO_BACK_REQUEST:
			close_searchbar()

func _process(delta):
	if !search_drag:
		$UI/SearchBar.rect_position.y = \
		lerp(
			$UI/SearchBar.rect_position.y,
			target_pos,
			delta * 10
		)
	if !floor_drag:
		$UI/FloorSlider/CurrentFloor.rect_position.y = \
		lerp(
			$UI/FloorSlider/CurrentFloor.rect_position.y,
			$UI/FloorSlider/Center.rect_position.y,
			 delta * 10
		)
	var v = $UI/FloorSlider.rect_size.y * 0.35
	$UI/FloorSlider/CurrentFloor/FloorBelow.self_modulate.a = \
	-($UI/FloorSlider/CurrentFloor.rect_position.y - $UI/FloorSlider/Center.rect_position.y) / v 
	$UI/FloorSlider/CurrentFloor/FloorAbove.self_modulate.a = \
	($UI/FloorSlider/CurrentFloor.rect_position.y - $UI/FloorSlider/Center.rect_position.y) / v 
	$UI/FloorSlider/CurrentFloor.self_modulate.a = \
	1 - abs($UI/FloorSlider/CurrentFloor.rect_position.y - $UI/FloorSlider/Center.rect_position.y) / v 

var floor_drag = false
func _on_FloorSlider_gui_input(event):
	if event is InputEventScreenTouch:
		var curr_floor = int($UI/FloorSlider/CurrentFloor.text)
		var max_floor = int(building_floors[-1].name)
		var min_floor = int(building_floors[0].name)
		if curr_floor + 1 < max_floor:
			$UI/FloorSlider/CurrentFloor/FloorAbove.text = str(curr_floor + 1)
			$UI/FloorSlider/CurrentFloor/FloorAbove.visible = true
		else:
			$UI/FloorSlider/CurrentFloor/FloorAbove.visible = false
		if curr_floor - 1 > min_floor:
			$UI/FloorSlider/CurrentFloor/FloorBelow.text = str(curr_floor - 1)
			$UI/FloorSlider/CurrentFloor/FloorBelow.visible = true
		else:
			$UI/FloorSlider/CurrentFloor/FloorBelow.visible = false
		if event.pressed:
			floor_drag = true
		else:
			floor_drag = false
	if event is InputEventScreenDrag:
		$UI/FloorSlider/CurrentFloor.rect_position.y += event.relative.y
		var v = $UI/FloorSlider.rect_size.y * 0.35
		var vv = v / 2
		var half = $UI/FloorSlider.rect_size.y * 0.5
		var max_floor = int(building_floors[-1].name)
		var min_floor = int(building_floors[0].name)
		if $UI/FloorSlider/CurrentFloor.rect_position.y > half + vv:
			var new_floor = int($UI/FloorSlider/CurrentFloor.text) + 1
			if max_floor >= new_floor:
				$UI/FloorSlider/CurrentFloor.text = str(new_floor)
				if new_floor + 1 < max_floor:
					$UI/FloorSlider/CurrentFloor/FloorAbove.text = str(new_floor + 1)
					$UI/FloorSlider/CurrentFloor/FloorAbove.visible = true
				else:
					$UI/FloorSlider/CurrentFloor/FloorAbove.visible = false
				if new_floor - 1 < max_floor:
					$UI/FloorSlider/CurrentFloor/FloorBelow.text = str(new_floor - 1)
					$UI/FloorSlider/CurrentFloor/FloorBelow.visible = true
				else:
					$UI/FloorSlider/CurrentFloor/FloorBelow.visible = false
				$UI/FloorSlider/CurrentFloor.rect_position.y -= v
				$CameraOrigin.set_floor(new_floor)
		elif $UI/FloorSlider/CurrentFloor.rect_position.y < half - vv:
			var new_floor = int($UI/FloorSlider/CurrentFloor.text) - 1
			if min_floor <= new_floor:
				$UI/FloorSlider/CurrentFloor.text = str(new_floor)
				if new_floor + 1 > min_floor:
					$UI/FloorSlider/CurrentFloor/FloorAbove.text = str(new_floor + 1)
					$UI/FloorSlider/CurrentFloor/FloorAbove.visible = true
				else:
					$UI/FloorSlider/CurrentFloor/FloorAbove.visible = false
				if new_floor - 1 > min_floor:
					$UI/FloorSlider/CurrentFloor/FloorBelow.text = str(new_floor - 1)
					$UI/FloorSlider/CurrentFloor/FloorBelow.visible = true
				else:
					$UI/FloorSlider/CurrentFloor/FloorBelow.visible = false
				$UI/FloorSlider/CurrentFloor.rect_position.y += v
				$CameraOrigin.set_floor(new_floor)
		else:
			pass

