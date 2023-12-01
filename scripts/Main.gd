extends Spatial


var groups = []
func _ready():
	$UI/SearchBar.rect_position.y = $UI.rect_size.y * 0.95
	PATHFINDER.call("Load", "data")
	#groups = PATHFINDER.call("Get_all_groups")
	#print(groups)

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
					target_pos = $UI.rect_size.y * 0.95
					$UI/VSlider.grab_focus()
					#$UI/SearchBar/Panel/LineEdit.focus
					#$UI/SearchBar/Panel/LineEdit2
				else:
					target_pos = 0
					$UI/SearchBar/Panel/From.grab_focus()
			else:
				print("b")
				if event.position.y > $UI.rect_size.y * 0.5:
					target_pos = $UI.rect_size.y * 0.95
					$UI/VSlider.grab_focus()
				else:
					target_pos = 0
					$UI/SearchBar/Panel/From.grab_focus()
