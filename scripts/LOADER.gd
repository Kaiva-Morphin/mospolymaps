extends Node

func _ready():
	#Load("SAVE")
	pass


func Load(path):
	var f = File.new()
	f.open(path, File.WRITE)
	#var content = f.get_as_text().split("\n")
	f.store_var([{"1":["2","2"],"2":["1","2"]}, {"1":["2","2"],"2":["1","2"]}], true)
	f.close()
	f.open(path, File.READ)
	var g = f.get_var(true)
	#var d = content[1]
	#var n = content[2]
	#var g_n = content[3]
	f.close()
	print(g)
	PATHFINDER.call("SetGraph", {})

func Save(val, path):
	print(val)
