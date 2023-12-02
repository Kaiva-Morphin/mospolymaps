extends Spatial


var groups = []
var ladder_groups = []
func _ready():
	var font = $UI/SearchBar/Panel/PREVIEW.get_font("font")
	font.size = get_viewport().size.x / 600 * 24
	$UI/SearchBar.rect_position.y = $UI.rect_size.y * 0.95
	parse_glb("res://resources/models/result.glb")
	PATHFINDER.call("Load", "res://MapData.save")
	
	groups = []
	ladder_groups = []
	var all_groups = PATHFINDER.call("Get_all_groups")
	for group in all_groups:
		if !"LADDER" in group:
			groups.append(group)
		else:
			ladder_groups.append(group)
	for l in ladder_groups:
		var holders = PATHFINDER.call("Get_group_members", l)
		if len(holders) < 2: continue
		var a = get_node(holders[0]) 
		var b = get_node(holders[1])
		var dist = a.translation.distance_to(b.translation)
		PATHFINDER.call("Register_bind", holders[0], holders[1], dist)
	
	
	
	
	

var building_floors = []
func parse_glb(path): # todo: autopath using raycasts
	var scene = load(path).instance()
	for c in scene.get_children():
		scene.remove_child(c)
		$Model.add_child(c)
	
	var mat = SpatialMaterial.new()
	mat.flags_transparent = true
	mat.albedo_color.a = 0.5
	
	$Model.get_node("FLOOR")
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
					to_materialize.material_override = mat
			building_floors.append(node)
		else:
			break
	
	$CameraOrigin.set_floor(1)



func _on_VSlider_changed():
	pass # Replace with function body.

func _on_VSlider_gui_input(event):
	$CameraOrigin.set_floor($UI/VSlider.value)


func _process(delta):
	if !holding:
		$UI/SearchBar.rect_position.y = lerp($UI/SearchBar.rect_position.y, target_pos, delta * 25)
var last_relative = Vector2.ZERO
onready var target_pos = $UI.rect_size.y * 0.95
var holding = false
func _on_SearchBar_gui_input(event):
	if event is InputEventScreenDrag: # todo: add multitouch support
		$UI/SearchBar.rect_position.y = clamp($UI/SearchBar.rect_position.y + event.relative.y, 0, $UI.rect_size.y * 0.95)
		last_relative = event.relative
		
	if event is InputEventScreenTouch:
		if event.pressed:
			holding = true
		else:
			holding = false
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
	$UI/VSlider.grab_focus()

func open_searchbar():
	target_pos = 0
	$UI/SearchBar/Panel/From.grab_focus()

func search_in_groups(s):
	update_search_colors()
	groups.sort()
	var results = []
	for group in groups:
		group = str(group)
		if group.to_lower().find(s.to_lower(), 0) == 0:
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

func _on_From_text_changed(new_text):
	selected[0] = false
	var res = search_in_groups(new_text)
	update_Scrolbar(res, true)

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
		$UI/VSlider.grab_focus() # todo: fix! затычка
	if event is InputEventScreenTouch:
		if !event.pressed:
			if last_select_relative.length_squared() < 25 and event.position.distance_squared_to(start_pos) < 25:
				if start_or_end:
					$UI/SearchBar/Panel/From.text = s
					$UI/VSlider.grab_focus()
					selected[0] = true
				else:
					$UI/SearchBar/Panel/To.text = s
					$UI/VSlider.grab_focus()
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
			var line = Path.new()
			line.set_script(line_script)
			line.global_coords = true
			line.default_color = Color.green
			line.resolution = 10
			line.width = 0.04
			line.curve = Curve3D.new()
			line.curve.add_point(get_node(path[p]).global_translation)
			line.curve.add_point(get_node(path[p-1]).global_translation)
			prev_line.append(line)
			get_node(path[p-1]).add_child(line)
	close_searchbar()
#6_LADDER_LEFT(7)
