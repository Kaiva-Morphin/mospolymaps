extends Spatial


# Declare member variables here. Examples:
# var a = 2
# var b = "text"


# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
#func _process(delta):
#	pass

onready var fl1 = $"result/1"
onready var fl2 = $"result/2"
onready var fl3 = $"result/3"
onready var fl4 = $"result/4"
onready var fl5 = $"result/5"
onready var fl6 = $"result/6"
onready var fl7 = $"result/7"
onready var fl8 = $"result/8"
func _on_VSlider_changed():
	match $UI/VSlider.value:
		1:
			pass
		2:
			pass
		3:
			pass
		4:
			pass
		5:
			pass
		6:
			pass
		7:
			pass
		8:
			pass
	pass # Replace with function body.
