
def Desc(py):
	content = dir(py)
	for i in range(len(content)):
		print i, ":", content[i]
