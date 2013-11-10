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
		void setScale(float scale)
		{
			for (std::vector<Feature>::iterator i = m_features.begin(); i < m_features.end(); ++i)
				i->setScale(scale);
		}

		float getVal(const Matrix &grayImage, const Matrix &squares, int i, int j) const;
	};

}