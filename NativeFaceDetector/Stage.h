#pragma once

#include "Matrix.h"
#include "Tree.h"

#include <vector>

namespace NativeFaceDetector
{

	class Stage
	{
		std::vector<Tree> m_trees;
		float             m_threshold;
	public:
		Stage(float threshold);

        void addTree(const Tree &t) { m_trees.push_back(t); }
		void setScale(float scale);
		bool pass(const Matrix &grayImage, const Matrix &squares, int i, int j) const;
	};

}
