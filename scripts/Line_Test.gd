extends Spatial

var line_script = load("res://Path.gd")
func _process(delta):
	var line = $Path
	line.set_script(line_script)
	line.global_coords = true
	line.resolution = 100
	line.width = 0.02
	line.curve = Curve3D.new()
	line.curve.add_point($Spatial2.global_translation)
	line.curve.add_point($Spatial.global_translation)
	
