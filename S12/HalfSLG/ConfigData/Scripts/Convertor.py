# coding=utf-8

import os
import xlrd
import Utility
import json
import types
import shutil

from shutil import *

#获取目录
dataPath = os.path.dirname(os.path.abspath(__file__)) + "/../Data/"
jsonPath = os.path.dirname(os.path.abspath(__file__)) + "/../Json/"
projectPath = os.path.dirname(os.path.abspath(__file__)) + "/../../HalfSLG/Assets/HalfSLG/Config//JsonConfig/"

print "dataPath = " + dataPath
print "jsonPath = " + jsonPath
print "projectPath = " + projectPath

#处理处理单个excel文件
def ProcessExcel(file):
	excel = xlrd.open_workbook( file )
	for i in range(excel.nsheets):
		#获得sheet
		table = excel.sheet_by_index(i)
		
		#获得sheet名字
		sheetName = table.name
		#如果名字是Utility则跳过
		if sheetName == "Utility":
			continue 

		print "Processing table . . . " + sheetName

		data = []
		#获得列数
		validCols = table.ncols
		#第2行是英文标题，因此从第三行开始
		for r in range(2, table.nrows):
			item = {}
			for c in range(validCols):
				#第1行是中文标题
				itemValue = table.cell(r, c).value
				#如果这个单元格的内容仍然是一个json，那么将这个字符串再转一次
				if isinstance(itemValue, basestring) and (itemValue.startswith('[') or itemValue.startswith('{')):
					itemValue = json.loads(itemValue)
					
				#表头就是这个key
				item[table.cell(1, c).value] = itemValue
			
			data.append(item)
		#保存到json文件
		sheetNamePath = jsonPath + sheetName + ".json"
		fo = open(sheetNamePath, "wb")
		#禁用ascii码写入，避免\u形式
		#使用indent格式，产生缩进
		jsonStr = json.dumps(data, ensure_ascii = False, indent = 2)
		fo.write(jsonStr.encode('utf-8'))
		fo.close()

#先删除已经存在的json
for path, docName, filesName in os.walk(jsonPath):
	for name in filesName:
		#仅处理json文件
		if ".json" in name:
			os.remove(jsonPath + name)

#遍历目录下所有的excel
for path, docName, filesName in os.walk(dataPath):
	for name in filesName:
		#仅处理excel文件(注意排除临时文件)
		if (".xlsx" in name or ".xls" in name) and "~$" not in name:
			ProcessExcel(dataPath + name)
	
	print "All Fin"

if not os.path.exists(projectPath):
	os.makedirs(projectPath)

#删除工程目录的文件
for path, docName, filesName in os.walk(projectPath):
	for name in filesName:
		#仅处理json文件
		if ".json" in name:
			os.remove(projectPath + name)

#将生成的.json拷贝到工程目录
for path, docName, filesName in os.walk(jsonPath):
	for name in filesName:
		#仅处理json文件
		if ".json" in name:
			copyfile(jsonPath + name, projectPath + name)


  
