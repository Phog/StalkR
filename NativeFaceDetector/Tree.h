#pragma once

#include "Feature.h"
#include "Matrix.h"
#include "Structures.h"

#include <vector>

namespace NativeFaceDetector
{

	class Tree
	{
		std::vector<Feature> m_features;
	public:
		void addFeature(const Feature &f) { m_features.push_back(f); }
		void setScale(float scale);
		float getVal(const Matrix &grayImage, const Matrix &squares, int i, int j) const;
	};

}